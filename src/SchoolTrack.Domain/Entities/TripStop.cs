using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Constants;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Operational progress waypoint for a trip stop, recording estimated and actual arrival/departure timestamps.
/// </summary>
public sealed class TripStop : TenantEntity
{
    public Guid TripId { get; set; }
    public Guid RouteStopId { get; set; }
    public int SequenceOrder { get; set; }
    public string Status { get; set; } = TripStopStatuses.Pending;
    public DateTimeOffset? EstimatedArrivalAt { get; set; }
    public DateTimeOffset? ActualArrivalAt { get; set; }
    public DateTimeOffset? ActualDepartureAt { get; set; }

    public Trip? Trip { get; set; }
    public RouteStop? RouteStop { get; set; }

    public void MarkArrived(DateTimeOffset utcNow)
    {
        Status = TripStopStatuses.Arrived;
        ActualArrivalAt = utcNow;
    }

    public void MarkDeparted(DateTimeOffset utcNow)
    {
        Status = TripStopStatuses.Departed;
        ActualDepartureAt = utcNow;
    }

    public void MarkSkipped()
    {
        Status = TripStopStatuses.Skipped;
    }
}

