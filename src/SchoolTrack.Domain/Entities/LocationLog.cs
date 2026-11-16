using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Immutable time-series telematics log of a bus's geographic coordinates, speed, and heading.
/// </summary>
public sealed class LocationLog : TenantEntity
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
    public DateTimeOffset RecordedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string? RawPayload { get; set; }

    public Bus? Bus { get; set; }
    public Trip? Trip { get; set; }
}

