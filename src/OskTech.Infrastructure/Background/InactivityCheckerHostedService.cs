using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Application.Options;
using OskTech.Domain.Enums;
using OskTech.Infrastructure.Cache;

namespace OskTech.Infrastructure.Background;

public sealed class InactivityCheckerHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<AuthOptions> authOptions,
    ILogger<InactivityCheckerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckInactiveUsersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Inactivity check failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CheckInactiveUsersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<RedisCacheService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = authOptions.Value.InactivityTimeout;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now - threshold;

        var inactiveUsers = db.Users
            .Where(x => x.LastActivityAt < cutoff)
            .Select(x => x.Id)
            .ToList();

        foreach (var userId in inactiveUsers)
        {
            var activeTokens = await refreshTokens.GetActiveByUserIdAsync(userId, ct);
            if (activeTokens.Count == 0)
                continue;

            await refreshTokens.RevokeAllByUserIdAsync(userId, now, ct);
            await outbox.AddAsync(Domain.Entities.OutboxMessage.Create(
                OutboxMessageTypes.SessionsRevoked,
                JsonSerializer.Serialize(new { UserId = userId }),
                now), ct);
            await cache.RevokeAllSessionsAsync(userId, ct);
            logger.LogInformation("Revoked inactive user {UserId}", userId);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
