namespace RedisStackDemo.Api.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ProductIndexName { get; set; } = "idx:products";
    public string ProductKeyPrefix { get; set; } = "product:";
    public string ChatChannel { get; set; } = "chat:general";
}
