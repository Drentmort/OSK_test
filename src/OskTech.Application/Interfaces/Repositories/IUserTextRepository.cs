using OskTech.Domain.Entities;

namespace OskTech.Application.Interfaces.Repositories;

public interface IUserTextRepository
{
    Task<UserText?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task UpsertAsync(UserText userText, CancellationToken ct);
}
