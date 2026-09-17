# RedisStackDemo

A .NET 8 / C# Web API showing how to build against a **Redis Stack** instance:
core Redis data structures via `StackExchange.Redis`, plus the RedisJSON and
RediSearch modules via `NRedisStack`.

## What's in the stack

| Feature | Redis capability | Client library | Code |
|---|---|---|---|
| Key/value cache with TTL | `SET` / `GET` / `EXPIRE` | StackExchange.Redis | `Services/CacheService.cs`, `Controllers/CacheController.cs` |
| Leaderboard / ranking | Sorted Sets (`ZINCRBY`, `ZREVRANGE`, `ZRANK`) | StackExchange.Redis | `Services/LeaderboardService.cs`, `Controllers/LeaderboardController.cs` |
| Pub/Sub broadcast | `PUBLISH` / `SUBSCRIBE` | StackExchange.Redis | `Services/ChatSubscriberService.cs`, `Controllers/ChatController.cs` |
| JSON document catalog | RedisJSON (`JSON.SET` / `JSON.GET`) | NRedisStack | `Services/ProductCatalogService.cs`, `Controllers/CatalogController.cs` |
| Full-text + faceted search | RediSearch (`FT.CREATE` / `FT.SEARCH`) | NRedisStack | same as above |

The API keeps a single `IConnectionMultiplexer` singleton for the process
(the correct pattern for StackExchange.Redis — never open one connection per
request), and every feature reuses it.

## Project layout

```
RedisStackDemo.sln
docker-compose.yml                     # redis-stack + the API
src/RedisStackDemo.Api/
  Program.cs                           # DI wiring, index bootstrap
  Options/RedisOptions.cs
  Models/                              # Product, LeaderboardEntry, ChatMessage
  Services/
    CacheService.cs                    # plain string cache
    LeaderboardService.cs              # sorted-set leaderboard
    ProductCatalogService.cs           # RedisJSON + RediSearch
    ChatSubscriberService.cs           # background Pub/Sub subscriber
  Controllers/
    CacheController.cs
    LeaderboardController.cs
    CatalogController.cs
    ChatController.cs
  Dockerfile
```

## Running it

You need the .NET 8 SDK and Docker on the machine you run this on (this
project was written in a sandboxed environment without access to nuget.org,
so `dotnet restore` has not been run here — do that first on your own
machine, where NuGet is reachable).

### 1. Start Redis Stack

```bash
docker compose up -d redis-stack
```

This runs `redis/redis-stack:latest`, which bundles RedisJSON, RediSearch,
RedisBloom and RedisTimeSeries, and exposes:
- `6379` — the Redis protocol
- `8001` — RedisInsight, a GUI you can open at http://localhost:8001 to browse
  keys, JSON documents and search indexes visually

### 2. Run the API

```bash
cd src/RedisStackDemo.Api
dotnet restore
dotnet run
```

By default it connects to `localhost:6379` (see `appsettings.json` →
`ConnectionStrings:Redis`). Swagger UI is available at `/swagger` in the
Development environment.

Alternatively, run everything (Redis Stack **and** the API) in Docker:

```bash
docker compose up --build
```

### 3. Try it out

```bash
# --- Cache ---
curl -X PUT localhost:8080/api/cache/greeting -H "Content-Type: application/json" \
  -d '{"value":"hello redis","ttlSeconds":60}'
curl localhost:8080/api/cache/greeting

# --- Leaderboard (sorted set) ---
curl -X POST localhost:8080/api/leaderboards/season1/alice/score -H "Content-Type: application/json" -d '{"increment":10}'
curl -X POST localhost:8080/api/leaderboards/season1/bob/score -H "Content-Type: application/json" -d '{"increment":25}'
curl localhost:8080/api/leaderboards/season1/top

# --- Pub/Sub ---
curl -X POST localhost:8080/api/chat -H "Content-Type: application/json" -d '{"from":"alice","text":"hi there"}'
curl localhost:8080/api/chat/recent

# --- JSON + Search catalog ---
curl -X POST localhost:8080/api/catalog -H "Content-Type: application/json" \
  -d '{"name":"Wireless Mouse","description":"A compact wireless mouse","category":"electronics","price":25.99}'
curl "localhost:8080/api/catalog/search?text=wireless&category=electronics&maxPrice=50"
```

(Use port `5xxx`/`https://localhost:7xxx` instead of `8080` when running via
`dotnet run` locally — check the console output or `Properties/launchSettings.json`
for the exact ports; `8080` is what the Docker Compose `api` service publishes.)

## Notes

- `ProductCatalogService.EnsureIndexAsync()` runs once at startup and creates
  the RediSearch index (`idx:products`) if it doesn't already exist. RediSearch
  then keeps the index in sync automatically with every `JSON.SET` / `JSON.DEL`
  under the `product:` key prefix — there's no separate indexing step to run.
- NuGet package versions in `RedisStackDemo.Api.csproj` (`StackExchange.Redis`,
  `NRedisStack`) were pinned from memory since this environment couldn't reach
  nuget.org to verify. If `dotnet restore` reports a version that doesn't
  exist, run `dotnet add package NRedisStack` / `dotnet add package
  StackExchange.Redis` without a version to pick up the latest, or check
  https://www.nuget.org for the current release.
