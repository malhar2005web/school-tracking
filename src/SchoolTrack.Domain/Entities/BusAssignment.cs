using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Operational assignment linking a driver to a specific bus for a shift or permanent schedule.
/// </summary>
public sealed class BusAssignment : TenantEntity
{
    public Guid BusId { get; set; }
    public Guid DriverId { get; set; }
    public DateTimeOffset AssignedFrom { get; set; }
    public DateTimeOffset? AssignedTo { get; set; }
    public bool IsCurrent { get; set; } = true;

    public Bus? Bus { get; set; }
    public Driver? Driver { get; set; }
}

