using RedisStackDemo.Api.Options;
using RedisStackDemo.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

// One multiplexer per process, shared by every feature below. StackExchange.Redis
// is designed to be used this way -- do not create a new connection per request.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var configuration = ConfigurationOptions.Parse(connectionString);
    configuration.AbortOnConnectFail = false; // keep retrying if Redis isn't up yet (e.g. container still starting)
    return ConnectionMultiplexer.Connect(configuration);
});

// Core Redis: string cache + sorted-set leaderboard.
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();

// Redis Stack modules: RedisJSON + RediSearch.
builder.Services.AddSingleton<IProductCatalogService, ProductCatalogService>();

// Redis Pub/Sub, kept subscribed for the app's lifetime.
builder.Services.AddSingleton<ChatSubscriberService>();
builder.Services.AddSingleton<IChatHistory>(sp => sp.GetRequiredService<ChatSubscriberService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ChatSubscriberService>());

var app = builder.Build();

// Make sure the RediSearch index exists before the app starts serving traffic.
using (var scope = app.Services.CreateScope())
{
    var catalog = scope.ServiceProvider.GetRequiredService<IProductCatalogService>();
    await catalog.EnsureIndexAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
