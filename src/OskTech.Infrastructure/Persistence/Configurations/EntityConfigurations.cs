using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OskTech.Domain.Entities;

namespace OskTech.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Login).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Login).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class UserTextConfiguration : IEntityTypeConfiguration<UserText>
{
    public void Configure(EntityTypeBuilder<UserText> builder)
    {
        builder.ToTable("user_texts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.DeviceId });
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => x.ProcessedAt);
    }
}
