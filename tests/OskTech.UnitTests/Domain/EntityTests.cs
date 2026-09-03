using OskTech.Domain.Entities;
using OskTech.Domain.Enums;

namespace OskTech.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Register_creates_user_with_activity()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Register("alice", "hash", now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("alice", user.Login);
        Assert.Equal(now, user.CreatedAt);
        Assert.Equal(now, user.LastActivityAt);
        Assert.False(user.IsInactive(now, TimeSpan.FromHours(24)));
    }

    [Fact]
    public void IsInactive_returns_true_after_threshold()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Register("alice", "hash", created);
        var now = created.AddHours(25);

        Assert.True(user.IsInactive(now, TimeSpan.FromHours(24)));
    }

    [Fact]
    public void UpdateActivity_updates_timestamp()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = created.AddMinutes(5);
        var user = User.Register("alice", "hash", created);

        user.UpdateActivity(updated);

        Assert.Equal(updated, user.LastActivityAt);
    }
}

public class UserTextTests
{
    [Fact]
    public void Create_and_Update_change_content()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var text = UserText.Create(userId, "hello", now);

        Assert.Equal("hello", text.Content);
        Assert.Equal(userId, text.UserId);

        var later = now.AddMinutes(1);
        text.Update("world", later);

        Assert.Equal("world", text.Content);
        Assert.Equal(later, text.UpdatedAt);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void Valid_token_is_not_expired_or_revoked()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", "device-1", now, now.AddDays(7));

        Assert.True(token.IsValid(now));
        Assert.False(token.IsExpired(now));
    }

    [Fact]
    public void Revoke_invalidates_token()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", "device-1", now, now.AddDays(7));

        token.Revoke(now.AddMinutes(1));

        Assert.False(token.IsValid(now.AddMinutes(2)));
        Assert.True(token.IsRevoked);
    }
}

public class OutboxMessageTests
{
    [Fact]
    public void Create_and_mark_processed()
    {
        var now = DateTime.UtcNow;
        var message = OutboxMessage.Create(OutboxMessageTypes.UserTextUpdated, "{}", now);

        Assert.Null(message.ProcessedAt);

        var processedAt = now.AddSeconds(1);
        message.MarkProcessed(processedAt);

        Assert.Equal(processedAt, message.ProcessedAt);
    }
}
