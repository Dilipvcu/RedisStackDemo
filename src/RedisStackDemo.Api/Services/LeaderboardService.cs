using RedisStackDemo.Api.Models;
using StackExchange.Redis;

namespace RedisStackDemo.Api.Services;

/// <summary>
/// A leaderboard backed by a single Redis Sorted Set. Sorted sets keep members
/// ordered by score server-side, so ranking/top-N queries are O(log N) instead
/// of requiring a client-side sort.
/// </summary>
public interface ILeaderboardService
{
    Task<double> IncrementScoreAsync(string board, string player, double increment);
    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count = 10);
    Task<LeaderboardEntry?> GetRankAsync(string board, string player);
}

public class LeaderboardService : ILeaderboardService
{
    private readonly IDatabase _db;

    public LeaderboardService(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    private static string Key(string board) => $"leaderboard:{board}";

    public Task<double> IncrementScoreAsync(string board, string player, double increment)
        => _db.SortedSetIncrementAsync(Key(board), player, increment);

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string board, int count = 10)
    {
        // Highest score first, with the 0-based rank of each entry.
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(
            Key(board), 0, count - 1, Order.Descending);

        return entries
            .Select((e, i) => new LeaderboardEntry
            {
                Player = e.Element!,
                Score = e.Score,
                Rank = i + 1
            })
            .ToList();
    }

    public async Task<LeaderboardEntry?> GetRankAsync(string board, string player)
    {
        var rank = await _db.SortedSetRankAsync(Key(board), player, Order.Descending);
        if (rank is null)
        {
            return null;
        }

        var score = await _db.SortedSetScoreAsync(Key(board), player);
        return new LeaderboardEntry
        {
            Player = player,
            Score = score ?? 0,
            Rank = rank.Value + 1
        };
    }
}
