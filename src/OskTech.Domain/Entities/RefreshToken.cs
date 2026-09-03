namespace OskTech.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string tokenHash, string deviceId, DateTime createdAt, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        if (expiresAt <= createdAt)
            throw new ArgumentException("Expiration must be after creation.", nameof(expiresAt));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            DeviceId = deviceId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke(DateTime now)
    {
        IsRevoked = true;
        RevokedAt = now;
    }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;

    public bool IsValid(DateTime now) => !IsRevoked && !IsExpired(now);
}
