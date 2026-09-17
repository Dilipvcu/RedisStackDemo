namespace RedisStackDemo.Api.Models;

/// <summary>
/// A message broadcast through Redis Pub/Sub (PUBLISH/SUBSCRIBE).
/// </summary>
public class ChatMessage
{
    public string From { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class PublishChatRequest
{
    public string From { get; set; } = "anonymous";
    public string Text { get; set; } = string.Empty;
}
