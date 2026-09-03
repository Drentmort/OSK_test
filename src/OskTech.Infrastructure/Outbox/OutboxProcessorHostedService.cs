using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Infrastructure.Cache;

namespace OskTech.Infrastructure.Outbox;

public sealed class OutboxProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox processing failed.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<RedisCacheService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var batch = await outbox.GetPendingAsync(50, ct);
        if (batch.Count == 0)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var message in batch)
        {
            try
            {
                await cache.ProcessOutboxMessageAsync(message.Type, message.Payload, ct);
                await outbox.MarkProcessedAsync(message.Id, now, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed outbox message {MessageId}", message.Id);
                await outbox.IncrementRetryAsync(message.Id, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
