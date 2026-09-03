using OskTech.Application.Interfaces;
using OskTech.Application.Interfaces.Repositories;

namespace OskTech.Application.Interfaces.Services;

public interface ICacheService
{
    Task<string?> GetUserTextAsync(Guid userId, CancellationToken ct);
    Task SetUserTextAsync(Guid userId, string content, CancellationToken ct);
    Task InvalidateUserTextAsync(Guid userId, CancellationToken ct);
    Task SetSessionAsync(Guid userId, string deviceId, string sessionId, TimeSpan ttl, CancellationToken ct);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct);
    Task RevokeSessionAsync(Guid userId, string deviceId, CancellationToken ct);
    Task<bool> IsSessionValidAsync(Guid userId, string deviceId, string sessionId, CancellationToken ct);
    Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window, CancellationToken ct);
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string login, string password, string deviceId, CancellationToken ct);
    Task<AuthResult> LoginAsync(string login, string password, string deviceId, CancellationToken ct);
    Task LogoutAsync(Guid userId, string deviceId, CancellationToken ct);
    Task LogoutAllDevicesAsync(Guid userId, CancellationToken ct);
}

public interface IUserTextService
{
    Task<string> GetTextAsync(Guid userId, CancellationToken ct);
    Task SaveTextAsync(Guid userId, string content, CancellationToken ct);
}

public interface IActivityService
{
    Task EnsureActiveAsync(Guid userId, CancellationToken ct);
    Task TouchActivityAsync(Guid userId, CancellationToken ct);
}

public sealed record AuthResult(Guid UserId, string Login, string RefreshToken, DateTime RefreshTokenExpiresAt);
