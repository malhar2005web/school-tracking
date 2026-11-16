using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Constants;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Immutable audit event recording a student boarding or deboarding a bus.
/// </summary>
public sealed class BoardingEvent : TenantEntity
{
    public Guid TripId { get; set; }
    public Guid StudentId { get; set; }
    public Guid RfidCardId { get; set; }
    public Guid? RouteStopId { get; set; }
    public Guid BusId { get; set; }
    public Guid RfidDeviceId { get; set; }
    public string ScanType { get; set; } = BoardingScanTypes.Boarding;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public bool IsManualOverride { get; set; }
    public string? Notes { get; set; }

    public Trip? Trip { get; set; }
    public Student? Student { get; set; }
    public RfidCard? RfidCard { get; set; }
    public RouteStop? RouteStop { get; set; }
    public Bus? Bus { get; set; }
    public RfidDevice? RfidDevice { get; set; }
}

