using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Constants;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Recurring schedule binding a route, vehicle, driver, and shift for automated trip instantiation.
/// </summary>
public sealed class RouteAssignment : TenantEntity
{
    public Guid RouteId { get; set; }
    public Guid BusId { get; set; }
    public Guid DriverId { get; set; }
    public string Shift { get; set; } = TripShifts.Morning;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public Route? Route { get; set; }
    public Bus? Bus { get; set; }
    public Driver? Driver { get; set; }
}

