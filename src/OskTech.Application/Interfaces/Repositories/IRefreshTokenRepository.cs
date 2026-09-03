using OskTech.Domain.Entities;

namespace OskTech.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> GetValidByHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
    Task RevokeAllByUserIdAsync(Guid userId, DateTime now, CancellationToken ct);
}
