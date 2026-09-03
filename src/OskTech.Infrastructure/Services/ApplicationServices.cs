using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Application.Interfaces.Services;
using OskTech.Application.Options;
using OskTech.Domain.Entities;
using OskTech.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace OskTech.Infrastructure.Services;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ICacheService cache,
    IPasswordHasher<User> passwordHasher,
    IOptions<AuthOptions> authOptions,
    IOptions<RateLimitOptions> rateLimitOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string login, string password, string deviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rateKey = $"rate:register:{login.ToLowerInvariant()}";
        if (!await cache.CheckRateLimitAsync(rateKey, rateLimitOptions.Value.RegisterPerMinute, TimeSpan.FromMinutes(1), ct))
            throw new InvalidOperationException("Too many registration attempts.");

        var existing = await users.GetByLoginAsync(login.Trim(), ct);
        if (existing is not null)
            throw new InvalidOperationException("Login already exists.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = User.Register(login, passwordHasher.HashPassword(null!, password), now);
        var (refreshToken, tokenEntity) = CreateRefreshToken(user.Id, deviceId, now);

        await users.AddAsync(user, ct);
        await refreshTokens.AddAsync(tokenEntity, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var sessionId = Guid.NewGuid().ToString("N");
        await cache.SetSessionAsync(user.Id, deviceId, sessionId, TimeSpan.FromDays(authOptions.Value.RefreshTokenDays), ct);

        logger.LogInformation("User registered {Login}", user.Login);
        return new AuthResult(user.Id, user.Login, refreshToken, tokenEntity.ExpiresAt);
    }

    public async Task<AuthResult> LoginAsync(string login, string password, string deviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rateKey = $"rate:login:{login.ToLowerInvariant()}";
        if (!await cache.CheckRateLimitAsync(rateKey, rateLimitOptions.Value.LoginPerMinute, TimeSpan.FromMinutes(1), ct))
            throw new InvalidOperationException("Too many login attempts.");

        var user = await users.GetByLoginAsync(login.Trim(), ct);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid login or password.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await EnsureUserActive(user, now, ct);

        user.UpdateActivity(now);
        var (refreshToken, tokenEntity) = CreateRefreshToken(user.Id, deviceId, now);

        await refreshTokens.AddAsync(tokenEntity, ct);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var sessionId = Guid.NewGuid().ToString("N");
        await cache.SetSessionAsync(user.Id, deviceId, sessionId, TimeSpan.FromDays(authOptions.Value.RefreshTokenDays), ct);

        return new AuthResult(user.Id, user.Login, refreshToken, tokenEntity.ExpiresAt);
    }

    public async Task LogoutAsync(Guid userId, string deviceId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokens = await refreshTokens.GetActiveByUserIdAsync(userId, ct);
        foreach (var token in tokens.Where(x => x.DeviceId == deviceId))
            token.Revoke(now);

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RevokeSessionAsync(userId, deviceId, ct);
    }

    public async Task LogoutAllDevicesAsync(Guid userId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await refreshTokens.RevokeAllByUserIdAsync(userId, now, ct);
        await outbox.AddAsync(OutboxMessage.Create(
            OutboxMessageTypes.SessionsRevoked,
            JsonSerializer.Serialize(new { UserId = userId }),
            now), ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RevokeAllSessionsAsync(userId, ct);
    }

    private (string Token, RefreshToken Entity) CreateRefreshToken(Guid userId, string deviceId, DateTime now)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(token);
        var expiresAt = now.AddDays(authOptions.Value.RefreshTokenDays);
        return (token, RefreshToken.Create(userId, hash, deviceId, now, expiresAt));
    }

    private async Task EnsureUserActive(User user, DateTime now, CancellationToken ct)
    {
        if (user.IsInactive(now, authOptions.Value.InactivityTimeout))
        {
            await refreshTokens.RevokeAllByUserIdAsync(user.Id, now, ct);
            await outbox.AddAsync(OutboxMessage.Create(
                OutboxMessageTypes.SessionsRevoked,
                JsonSerializer.Serialize(new { UserId = user.Id }),
                now), ct);
            await unitOfWork.SaveChangesAsync(ct);
            await cache.RevokeAllSessionsAsync(user.Id, ct);
            throw new InvalidOperationException("Session expired due to inactivity.");
        }
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

public sealed class UserTextService(
    IUserRepository users,
    IUserTextRepository userTexts,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ICacheService cache,
    IActivityService activity,
    TimeProvider timeProvider) : IUserTextService
{
    public async Task<string> GetTextAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await activity.EnsureActiveAsync(userId, ct);

        var cached = await cache.GetUserTextAsync(userId, ct);
        if (cached is not null)
            return cached;

        var text = await userTexts.GetByUserIdAsync(userId, ct);
        var content = text?.Content ?? string.Empty;
        await cache.SetUserTextAsync(userId, content, ct);
        return content;
    }

    public async Task SaveTextAsync(Guid userId, string content, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await activity.EnsureActiveAsync(userId, ct);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("User not found.");
        user.UpdateActivity(now);

        var existing = await userTexts.GetByUserIdAsync(userId, ct);
        if (existing is null)
            await userTexts.UpsertAsync(UserText.Create(userId, content, now), ct);
        else
        {
            existing.Update(content, now);
            await userTexts.UpsertAsync(existing, ct);
        }

        await users.UpdateAsync(user, ct);
        await outbox.AddAsync(OutboxMessage.Create(
            OutboxMessageTypes.UserTextUpdated,
            JsonSerializer.Serialize(new { UserId = userId, Content = content }),
            now), ct);
        await outbox.AddAsync(OutboxMessage.Create(
            OutboxMessageTypes.ActivityUpdated,
            JsonSerializer.Serialize(new { UserId = userId, LastActivityAt = now }),
            now), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class ActivityService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ICacheService cache,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider) : IActivityService
{
    public async Task EnsureActiveAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("User not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (!user.IsInactive(now, authOptions.Value.InactivityTimeout))
            return;

        await refreshTokens.RevokeAllByUserIdAsync(userId, now, ct);
        await outbox.AddAsync(OutboxMessage.Create(
            OutboxMessageTypes.SessionsRevoked,
            JsonSerializer.Serialize(new { UserId = userId }),
            now), ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RevokeAllSessionsAsync(userId, ct);
        throw new InvalidOperationException("Session expired due to inactivity.");
    }

    public async Task TouchActivityAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        user.UpdateActivity(now);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
