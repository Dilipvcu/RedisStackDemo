using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RedisStackDemo.Api.Models;
using RedisStackDemo.Api.Options;
using StackExchange.Redis;

namespace RedisStackDemo.Api.Services;

/// <summary>
/// Keeps a small in-memory ring buffer of the most recent messages published on
/// the chat channel, so the API can expose "what was recently said" over plain
/// HTTP even though Pub/Sub itself has no history or replay.
/// </summary>
public interface IChatHistory
{
    IReadOnlyList<ChatMessage> Recent(int count = 20);
}

/// <summary>
/// A hosted service that subscribes once at startup (SUBSCRIBE) and stays
/// subscribed for the app's lifetime, demonstrating Redis Pub/Sub fan-out:
/// any number of processes can PUBLISH to the same channel and every
/// subscriber -- this one included -- receives every message.
/// </summary>
public class ChatSubscriberService : IHostedService, IChatHistory
{
    private readonly IConnectionMultiplexer _mux;
    private readonly RedisOptions _options;
    private readonly ILogger<ChatSubscriberService> _logger;
    private readonly ConcurrentQueue<ChatMessage> _recent = new();
    private const int MaxHistory = 50;

    public ChatSubscriberService(
        IConnectionMultiplexer mux,
        IOptions<RedisOptions> options,
        ILogger<ChatSubscriberService> logger)
    {
        _mux = mux;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var subscriber = _mux.GetSubscriber();
        var channel = RedisChannel.Literal(_options.ChatChannel);

        await subscriber.SubscribeAsync(channel, (_, message) =>
        {
            try
            {
                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(message!);
                if (chatMessage is null)
                {
                    return;
                }

                _recent.Enqueue(chatMessage);
                while (_recent.Count > MaxHistory && _recent.TryDequeue(out _))
                {
                    // trim ring buffer
                }

                _logger.LogInformation("[{Channel}] {From}: {Text}",
                    _options.ChatChannel, chatMessage.From, chatMessage.Text);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Received malformed chat message on {Channel}", _options.ChatChannel);
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlyList<ChatMessage> Recent(int count = 20) =>
        _recent.Reverse().Take(count).ToList();
}
