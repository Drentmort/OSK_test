using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OskTech.Application.Interfaces.Repositories;
using OskTech.Application.Interfaces.Services;
using OskTech.Application.Options;
using OskTech.Domain.Entities;
using OskTech.Infrastructure.Background;
using OskTech.Infrastructure.Cache;
using OskTech.Infrastructure.Outbox;
using OskTech.Infrastructure.Persistence;
using OskTech.Infrastructure.Persistence.Repositories;
using OskTech.Infrastructure.Services;
using StackExchange.Redis;

namespace OskTech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool registerHostedServices = true)
    {
        services.AddOptions<AuthOptions>().Bind(configuration.GetSection(AuthOptions.SectionName));
        services.AddOptions<RateLimitOptions>().Bind(configuration.GetSection(RateLimitOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTextRepository, UserTextRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<RedisCacheService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserTextService, UserTextService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        if (registerHostedServices)
        {
            services.AddHostedService<OutboxProcessorHostedService>();
            services.AddHostedService<InactivityCheckerHostedService>();
        }

        return services;
    }
}
