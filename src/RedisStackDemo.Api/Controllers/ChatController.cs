using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using RedisStackDemo.Api.Models;
using RedisStackDemo.Api.Options;
using RedisStackDemo.Api.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace RedisStackDemo.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IChatHistory _history;
    private readonly RedisOptions _options;

    public ChatController(IConnectionMultiplexer mux, IChatHistory history, IOptions<RedisOptions> options)
    {
        _mux = mux;
        _history = history;
        _options = options.Value;
    }

    /// <summary>
    /// Publishes a message to the shared chat channel (PUBLISH). Every subscriber
    /// currently listening -- including this same process's background
    /// subscriber, and any other instance of this API running elsewhere -- gets
    /// it immediately.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishChatRequest request)
    {
        var message = new ChatMessage { From = request.From, Text = request.Text };
        var payload = JsonSerializer.Serialize(message);

        var channel = RedisChannel.Literal(_options.ChatChannel);
        var subscriberCount = await _mux.GetSubscriber().PublishAsync(channel, payload);

        return Ok(new { published = true, receivedBy = subscriberCount });
    }

    [HttpGet("recent")]
    public IActionResult Recent([FromQuery] int count = 20)
    {
        return Ok(_history.Recent(count));
    }
}
"}}
