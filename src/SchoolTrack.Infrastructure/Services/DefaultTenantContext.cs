using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.Infrastructure.Services;

/// <summary>
/// Default tenant context implementation used before HTTP JWT claim binding is enabled in Phase 2.
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    /// <inheritdoc />
    public Guid? SchoolId => null;

    /// <inheritdoc />
    public bool IsPlatformAdmin => false;
}

