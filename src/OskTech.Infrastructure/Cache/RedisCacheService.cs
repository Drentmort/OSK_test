using System.Text.Json;
using Microsoft.Extensions.Logging;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Application.Interfaces.Services;
using OskTech.Domain.Enums;
using StackExchange.Redis;

namespace OskTech.Infrastructure.Cache;

public sealed class RedisCacheService(
    IConnectionMultiplexer multiplexer,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private IDatabase Db => multiplexer.GetDatabase();

    public async Task<string?> GetUserTextAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var value = await Db.StringGetAsync(UserTextKey(userId));
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetUserTextAsync(Guid userId, string content, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Db.StringSetAsync(UserTextKey(userId), content, TimeSpan.FromHours(24));
    }

    public Task InvalidateUserTextAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Db.KeyDeleteAsync(UserTextKey(userId));
    }

    public Task SetSessionAsync(Guid userId, string deviceId, string sessionId, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Db.StringSetAsync(SessionKey(userId, deviceId), sessionId, ttl);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var server = multiplexer.GetServer(multiplexer.GetEndPoints().First());
        var pattern = $"session:{userId}:*";
        await foreach (var key in server.KeysAsync(pattern: pattern))
            await Db.KeyDeleteAsync(key);

        await InvalidateUserTextAsync(userId, ct);
        logger.LogInformation("Revoked sessions for user {UserId}", userId);
    }

    public Task RevokeSessionAsync(Guid userId, string deviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Db.KeyDeleteAsync(SessionKey(userId, deviceId));
    }

    public async Task<bool> IsSessionValidAsync(Guid userId, string deviceId, string sessionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var stored = await Db.StringGetAsync(SessionKey(userId, deviceId));
        return stored.HasValue && stored.ToString() == sessionId;
    }

    public async Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var count = await Db.StringIncrementAsync(key);
        if (count == 1)
            await Db.KeyExpireAsync(key, window);

        return count <= limit;
    }

    internal async Task ProcessOutboxMessageAsync(string type, string payload, CancellationToken ct)
    {
        switch (type)
        {
            case OutboxMessageTypes.UserTextUpdated:
            {
                var data = JsonSerializer.Deserialize<UserTextPayload>(payload, JsonOptions);
                if (data is not null)
                    await SetUserTextAsync(data.UserId, data.Content, ct);
                break;
            }
            case OutboxMessageTypes.SessionsRevoked:
            {
                var data = JsonSerializer.Deserialize<UserIdPayload>(payload, JsonOptions);
                if (data is not null)
                    await RevokeAllSessionsAsync(data.UserId, ct);
                break;
            }
            case OutboxMessageTypes.ActivityUpdated:
                break;
        }
    }

    private static RedisKey UserTextKey(Guid userId) => $"cache:text:{userId}";
    private static RedisKey SessionKey(Guid userId, string deviceId) => $"session:{userId}:{deviceId}";

    private sealed record UserTextPayload(Guid UserId, string Content);
    private sealed record UserIdPayload(Guid UserId);
}
