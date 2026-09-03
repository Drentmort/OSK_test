using OskTech.Domain.Entities;

namespace OskTech.Application.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct);
    Task MarkProcessedAsync(Guid id, DateTime now, CancellationToken ct);
    Task IncrementRetryAsync(Guid id, CancellationToken ct);
}
