using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.Infrastructure.Persistence.Context;

/// <summary>
/// Primary Entity Framework Core database context for the SchoolTrack platform with multi-tenant query filtering.
/// </summary>
public sealed class SchoolTrackDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SchoolTrackDbContext(
        DbContextOptions<SchoolTrackDbContext> options,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Multi-tenant schools registered on the platform.
    /// </summary>
    public DbSet<School> Schools => Set<School>();

    /// <summary>
    /// Operational and hardware scan configuration per school.
    /// </summary>
    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();

    /// <summary>
    /// Platform and school administrators, drivers, and parents.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Cryptographically hashed refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Immutable security and business audit logs.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<BusAssignment> BusAssignments => Set<BusAssignment>();
    public DbSet<RfidCard> RfidCards => Set<RfidCard>();
    public DbSet<GpsDevice> GpsDevices => Set<GpsDevice>();
    public DbSet<RfidDevice> RfidDevices => Set<RfidDevice>();

    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripStop> TripStops => Set<TripStop>();
    public DbSet<TripStatusHistory> TripStatusHistories => Set<TripStatusHistory>();

    public DbSet<LocationLog> LocationLogs => Set<LocationLog>();
    public DbSet<LatestBusLocation> LatestBusLocations => Set<LatestBusLocation>();

    public DbSet<BoardingEvent> BoardingEvents => Set<BoardingEvent>();
    public DbSet<UnknownCardTap> UnknownCardTaps => Set<UnknownCardTap>();

    /// <summary>
    /// Exposes the active tenant context for query filters in tenant-scoped entities.
    /// </summary>
    public Guid? CurrentSchoolId => _tenantContext.SchoolId;

    /// <summary>
    /// Indicates whether the current caller is a platform-wide administrator.
    /// </summary>
    public bool IsPlatformAdmin => _tenantContext.IsPlatformAdmin;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolTrackDbContext).Assembly);

        // Apply global multi-tenant isolation filters
        modelBuilder.Entity<SchoolSettings>()
            .HasQueryFilter(s => _tenantContext.IsPlatformAdmin || _tenantContext.SchoolId == null || s.SchoolId == _tenantContext.SchoolId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => _tenantContext.IsPlatformAdmin || _tenantContext.SchoolId == null || u.SchoolId == null || u.SchoolId == _tenantContext.SchoolId);

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(a => _tenantContext.IsPlatformAdmin || _tenantContext.SchoolId == null || a.SchoolId == null || a.SchoolId == _tenantContext.SchoolId);

        // For all current and future entities extending TenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var schoolIdProperty = Expression.Property(parameter, nameof(TenantEntity.SchoolId));

                // Expression: _tenantContext.IsPlatformAdmin || _tenantContext.SchoolId == null || e.SchoolId == _tenantContext.SchoolId.Value
                var isPlatformAdminExpr = Expression.Property(Expression.Constant(this), nameof(IsPlatformAdmin));
                var currentSchoolIdExpr = Expression.Property(Expression.Constant(this), nameof(CurrentSchoolId));
                var nullGuidExpr = Expression.Constant(null, typeof(Guid?));
                var schoolIdNullable = Expression.Convert(schoolIdProperty, typeof(Guid?));

                var isNullOrAdmin = Expression.OrElse(
                    isPlatformAdminExpr,
                    Expression.Equal(currentSchoolIdExpr, nullGuidExpr));

                var matchesTenant = Expression.Equal(schoolIdNullable, currentSchoolIdExpr);
                var body = Expression.OrElse(isNullOrAdmin, matchesTenant);

                var lambda = Expression.Lambda(body, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditTimestamps()
    {
        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.CreateVersion7();
                }

                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
