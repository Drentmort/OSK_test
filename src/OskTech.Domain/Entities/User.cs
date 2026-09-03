namespace OskTech.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private User()
    {
    }

    public static User Register(string login, string passwordHash, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = now,
            LastActivityAt = now
        };
    }

    public void UpdateActivity(DateTime now) => LastActivityAt = now;

    public bool IsInactive(DateTime now, TimeSpan threshold) => now - LastActivityAt > threshold;
}
