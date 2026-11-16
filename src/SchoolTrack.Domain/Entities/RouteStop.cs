using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Ordered pickup/drop-off waypoint along a designated bus route with geographic coordinates and geofence.
/// </summary>
public sealed class RouteStop : TenantEntity
{
    public Guid RouteId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int ScheduledOffsetMinutes { get; set; }
    public int GeofenceRadiusMeters { get; set; } = 50;

    public Route? Route { get; set; }
    public ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
}

