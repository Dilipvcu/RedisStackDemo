using System.Text.Json.Serialization;

namespace RedisStackDemo.Api.Models;

/// <summary>
/// A catalog item stored as a native JSON document in Redis (via the RedisJSON module)
/// and indexed for full-text / faceted search (via the RediSearch module).
/// </summary>
public class Product
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("inStock")]
    public bool InStock { get; set; } = true;
}
