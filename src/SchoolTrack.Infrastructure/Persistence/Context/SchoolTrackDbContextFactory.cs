using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchoolTrack.Infrastructure.Services;

namespace SchoolTrack.Infrastructure.Persistence.Context;

/// <summary>
/// Design-time factory for EF Core CLI migrations without requiring full application startup.
/// </summary>
public sealed class SchoolTrackDbContextFactory : IDesignTimeDbContextFactory<SchoolTrackDbContext>
{
    private const string LocalFallbackConnectionString =
        "Host=localhost;Port=5432;Database=schooltrack_dev;Username=postgres;Password=postgres";

    /// <inheritdoc />
    public SchoolTrackDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("SCHOOLTRACK_DB_CONNECTION")
            ?? LocalFallbackConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<SchoolTrackDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(SchoolTrackDbContext).Assembly.FullName));

        return new SchoolTrackDbContext(
            optionsBuilder.Options,
            new DefaultTenantContext(),
            new SystemDateTimeProvider());
    }
}

