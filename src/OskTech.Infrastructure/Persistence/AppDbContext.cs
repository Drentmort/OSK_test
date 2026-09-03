using Microsoft.EntityFrameworkCore;
using OskTech.Domain.Entities;

namespace OskTech.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserText> UserTexts => Set<UserText>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
