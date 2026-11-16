using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Fast single-row-per-bus projection of the most recent geographic coordinates, speed, heading, and online status.
/// </summary>
public sealed class LatestBusLocation : TenantEntity
{
    public Guid BusId { get; set; }
    public Guid? TripId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal SpeedKmh { get; set; }
    public decimal Heading { get; set; }
    public decimal? Altitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public bool IgnitionOn { get; set; } = true;
    public bool IsOnline { get; set; } = true;
    public DateTimeOffset RecordedAt { get; set; }

    public Bus? Bus { get; set; }
    public Trip? Trip { get; set; }
}

