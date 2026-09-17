namespace RedisStackDemo.Api.Models;

/// <summary>
/// One row of a leaderboard backed by a Redis Sorted Set (ZADD/ZINCRBY/ZREVRANGE).
/// </summary>
public class LeaderboardEntry
{
    public string Player { get; set; } = string.Empty;
    public double Score { get; set; }
    public long Rank { get; set; }
}

public class ScoreUpdateRequest
{
    public double Increment { get; set; }
}
