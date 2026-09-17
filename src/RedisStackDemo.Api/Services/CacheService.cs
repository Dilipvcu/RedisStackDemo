using System.Text.Json;
using StackExchange.Redis;

namespace RedisStackDemo.Api.Services;

/// <summary>
/// Plain string cache (GET/SET/EXPIRE/DEL) via the core StackExchange.Redis client.
/// This is the "classic Redis" part of the stack -- no Redis Stack modules involved.
/// </summary>
public interface ICacheService
{
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}

public class CacheService : ICacheService
{
    private readonly IDatabase _db;

    public CacheService(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(value);
        return await _db.StringSetAsync(key, json, ttl);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!);
    }

    public Task<bool> DeleteAsync(string key) => _db.KeyDeleteAsync(key);

    public Task<bool> ExistsAsync(string key) => _db.KeyExistsAsync(key);
}
