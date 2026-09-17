using Microsoft.AspNetCore.Mvc;
using RedisStackDemo.Api.Services;

namespace RedisStackDemo.Api.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cache;

    public CacheController(ICacheService cache)
    {
        _cache = cache;
    }

    public record SetCacheRequest(string Value, int? TtlSeconds);

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, [FromBody] SetCacheRequest request)
    {
        TimeSpan? ttl = request.TtlSeconds.HasValue
            ? TimeSpan.FromSeconds(request.TtlSeconds.Value)
            : null;

        var ok = await _cache.SetAsync(key, request.Value, ttl);
        return ok ? Ok(new { key, cached = true, ttlSeconds = request.TtlSeconds }) : Problem("Failed to write to cache");
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var value = await _cache.GetAsync<string>(key);
        return value is null ? NotFound() : Ok(new { key, value });
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        var deleted = await _cache.DeleteAsync(key);
        return deleted ? NoContent() : NotFound();
    }
}
