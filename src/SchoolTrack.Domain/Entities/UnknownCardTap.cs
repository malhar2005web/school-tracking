using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Security audit record for unknown, unregistered, or unassigned RFID card scans.
/// </summary>
public sealed class UnknownCardTap : TenantEntity
{
    public Guid? BusId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? RfidDeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string CardUid { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; // UnregisteredCard, UnassignedCard, NoActiveTrip
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset ScannedAt { get; set; }

    public Bus? Bus { get; set; }
    public Trip? Trip { get; set; }
    public RfidDevice? RfidDevice { get; set; }
}

