using Microsoft.AspNetCore.Mvc;
using RedisStackDemo.Api.Models;
using RedisStackDemo.Api.Services;

namespace RedisStackDemo.Api.Controllers;

[ApiController]
[Route("api/leaderboards/{board}")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboard;

    public LeaderboardController(ILeaderboardService leaderboard)
    {
        _leaderboard = leaderboard;
    }

    [HttpPost("{player}/score")]
    public async Task<IActionResult> IncrementScore(string board, string player, [FromBody] ScoreUpdateRequest request)
    {
        var newScore = await _leaderboard.IncrementScoreAsync(board, player, request.Increment);
        return Ok(new { board, player, score = newScore });
    }

    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntry>>> Top(string board, [FromQuery] int count = 10)
    {
        var entries = await _leaderboard.GetTopAsync(board, count);
        return Ok(entries);
    }

    [HttpGet("{player}")]
    public async Task<IActionResult> GetRank(string board, string player)
    {
        var entry = await _leaderboard.GetRankAsync(board, player);
        return entry is null ? NotFound() : Ok(entry);
    }
}
