namespace OskTech.Domain.Entities;

public class UserText
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private UserText()
    {
    }

    public static UserText Create(Guid userId, string content, DateTime now)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        return new UserText
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = content ?? string.Empty,
            UpdatedAt = now
        };
    }

    public void Update(string content, DateTime now)
    {
        Content = content ?? string.Empty;
        UpdatedAt = now;
    }
}
