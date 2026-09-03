using Microsoft.EntityFrameworkCore;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Domain.Entities;
using OskTech.Infrastructure.Persistence;

namespace OskTech.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<User?> GetByLoginAsync(string login, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Login == login, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }
}

public sealed class UserTextRepository(AppDbContext db) : IUserTextRepository
{
    public Task<UserText?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        db.UserTexts.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task UpsertAsync(UserText userText, CancellationToken ct)
    {
        var existing = await db.UserTexts.FirstOrDefaultAsync(x => x.UserId == userText.UserId, ct);
        if (existing is null)
        {
            await db.UserTexts.AddAsync(userText, ct);
            return;
        }

        existing.Update(userText.Content, userText.UpdatedAt);
    }
}

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetValidByHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsRevoked, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct) =>
        await db.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked).ToListAsync(ct);

    public async Task RevokeAllByUserIdAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked).ToListAsync(ct);
        foreach (var token in tokens)
            token.Revoke(now);
    }
}

public sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken ct) =>
        await db.OutboxMessages.AddAsync(message, ct);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct) =>
        await db.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task MarkProcessedAsync(Guid id, DateTime now, CancellationToken ct)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (message is not null)
            message.MarkProcessed(now);
    }

    public async Task IncrementRetryAsync(Guid id, CancellationToken ct)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (message is not null)
            message.IncrementRetry();
    }
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
