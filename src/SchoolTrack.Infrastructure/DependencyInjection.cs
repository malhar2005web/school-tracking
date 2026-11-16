using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Authentication;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;

namespace SchoolTrack.Infrastructure;

/// <summary>
/// Registers PostgreSQL persistence, authentication tokens, tenant services, and hashing infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds SchoolTrack Infrastructure layer dependencies to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'ConnectionStrings:DefaultConnection' is not configured.");
        }

        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddDbContext<SchoolTrackDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(SchoolTrackDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SchoolTrackDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SchoolTrackDbContext>());

        return services;
    }
}
