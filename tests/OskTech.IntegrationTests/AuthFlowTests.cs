using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OskTech.Application.Interfaces.Services;
using OskTech.Infrastructure;
using OskTech.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace OskTech.IntegrationTests;

public sealed class AuthFlowTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private ServiceProvider? _provider;
    private bool _initialized;

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("osktech")
                .WithUsername("osk")
                .WithPassword("osk")
                .Build();

            _redis = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await _postgres.StartAsync();
            await _redis.StartAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                    ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                    ["Auth:RefreshTokenDays"] = "7",
                    ["Auth:InactivityTimeout"] = "24:00:00",
                    ["RateLimit:LoginPerMinute"] = "100",
                    ["RateLimit:RegisterPerMinute"] = "100"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddDebug());
            services.AddInfrastructure(configuration, registerHostedServices: false);

            _provider = services.BuildServiceProvider();

            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            _initialized = true;
        }
        catch (ArgumentException)
        {
            _initialized = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();

        if (_redis is not null)
            await _redis.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [SkippableFact]
    public async Task Register_login_save_text_flow_works()
    {
        Skip.IfNot(_initialized, "Docker is not available.");

        using var scope = _provider!.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var texts = scope.ServiceProvider.GetRequiredService<IUserTextService>();
        var ct = CancellationToken.None;

        var registered = await auth.RegisterAsync("integration_user", "password123", "device-1", ct);
        Assert.Equal("integration_user", registered.Login);

        var loggedIn = await auth.LoginAsync("integration_user", "password123", "device-1", ct);
        Assert.Equal(registered.UserId, loggedIn.UserId);

        await texts.SaveTextAsync(registered.UserId, "hello integration", ct);
        var content = await texts.GetTextAsync(registered.UserId, ct);
        Assert.Equal("hello integration", content);

        await auth.LogoutAllDevicesAsync(registered.UserId, ct);
    }
}
