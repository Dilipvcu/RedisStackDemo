FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RedisStackDemo.Api.csproj .
RUN dotnet restore RedisStackDemo.Api.csproj

COPY . .
RUN dotnet publish RedisStackDemo.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "RedisStackDemo.Api.dll"]
