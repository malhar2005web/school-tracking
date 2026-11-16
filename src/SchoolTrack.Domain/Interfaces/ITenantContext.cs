namespace SchoolTrack.Domain.Interfaces;

/// <summary>
/// Resolves the current authenticated school tenant scope for request and query filtering.
/// </summary>
public interface ITenantContext
{
    Guid? SchoolId { get; }
    bool IsPlatformAdmin { get; }
}

