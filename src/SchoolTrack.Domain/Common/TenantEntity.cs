namespace SchoolTrack.Domain.Common;

/// <summary>
/// Base entity for all school-scoped records enforcing multi-tenant isolation.
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid SchoolId { get; set; }
}

