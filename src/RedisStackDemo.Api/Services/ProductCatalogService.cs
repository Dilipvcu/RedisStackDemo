using Microsoft.Extensions.Options;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using RedisStackDemo.Api.Models;
using RedisStackDemo.Api.Options;
using StackExchange.Redis;

namespace RedisStackDemo.Api.Services;

/// <summary>
/// Stores products as native JSON documents (RedisJSON) and keeps them queryable
/// through a RediSearch index built directly on those documents -- no separate
/// search engine, no ETL. This is the "Redis Stack" part proper (JSON + Search
/// modules), as opposed to core Redis data structures.
/// </summary>
public interface IProductCatalogService
{
    Task EnsureIndexAsync();
    Task<Product> UpsertAsync(Product product);
    Task<Product?> GetAsync(string id);
    Task<bool> DeleteAsync(string id);
    Task<IReadOnlyList<Product>> SearchAsync(string? text, string? category, double? maxPrice, int limit = 20);
}

public class ProductCatalogService : IProductCatalogService
{
    private readonly IDatabase _db;
    private readonly RedisOptions _options;
    private readonly ILogger<ProductCatalogService> _logger;

    public ProductCatalogService(
        IConnectionMultiplexer mux,
        IOptions<RedisOptions> options,
        ILogger<ProductCatalogService> logger)
    {
        _db = mux.GetDatabase();
        _options = options.Value;
        _logger = logger;
    }

    private string KeyFor(string id) => $"{_options.ProductKeyPrefix}{id}";

    /// <summary>
    /// Creates the RediSearch index if it doesn't already exist. Safe to call on
    /// every startup. The index is defined once and Redis keeps it in sync with
    /// every JSON.SET/DEL under the given key prefix automatically.
    /// </summary>
    public async Task EnsureIndexAsync()
    {
        try
        {
            await _db.FT().InfoAsync(_options.ProductIndexName);
            return; // already exists
        }
        catch (RedisServerException)
        {
            // Index doesn't exist yet -- fall through and create it.
        }

        var schema = new Schema()
            .AddTextField(new FieldName("$.name", "name"), weight: 2.0)
            .AddTextField(new FieldName("$.description", "description"))
            .AddTagField(new FieldName("$.category", "category"))
            .AddNumericField(new FieldName("$.price", "price"));

        var createParams = new FTCreateParams()
            .On(IndexDataType.JSON)
            .Prefix(_options.ProductKeyPrefix);

        await _db.FT().CreateAsync(_options.ProductIndexName, createParams, schema);
        _logger.LogInformation("Created RediSearch index {Index}", _options.ProductIndexName);
    }

    public async Task<Product> UpsertAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Id))
        {
            product.Id = Guid.NewGuid().ToString("N");
        }

        await _db.JSON().SetAsync(KeyFor(product.Id), "$", product);
        return product;
    }

    public async Task<Product?> GetAsync(string id)
    {
        return await _db.JSON().GetAsync<Product>(KeyFor(id));
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await _db.JSON().DelAsync(KeyFor(id));
        return deleted > 0;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? text, string? category, double? maxPrice, int limit = 20)
    {
        var clauses = new List<string> { "*" };

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Free-text search across name/description.
            clauses[0] = text!;
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            clauses.Add($"@category:{{{EscapeTag(category!)}}}");
        }

        if (maxPrice.HasValue)
        {
            clauses.Add($"@price:[-inf {maxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");
        }

        var queryString = string.Join(" ", clauses);
        var query = new Query(queryString).Limit(0, limit);

        var result = await _db.FT().SearchAsync(_options.ProductIndexName, query);

        // For a JSON-backed index, FT.SEARCH returns the full document under the "$" field.
        return result.Documents
            .Select(doc =>
            {
                var json = doc["$"].ToString();
                return System.Text.Json.JsonSerializer.Deserialize<Product>(json!)!;
            })
            .ToList();
    }

    private static string EscapeTag(string value) =>
        value.Replace(" ", "\\ ").Replace("-", "\\-");
}
