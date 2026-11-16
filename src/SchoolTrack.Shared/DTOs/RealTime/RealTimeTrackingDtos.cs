namespace SchoolTrack.Shared.DTOs.RealTime;

/// <summary>
/// Real-time event pushed to parents and school monitors when a student scans their RFID card.
/// </summary>
public sealed record BoardingNotificationDto(
    Guid EventId,
    Guid SchoolId,
    Guid TripId,
    string RouteName,
    Guid BusId,
    string BusNumber,
    Guid StudentId,
    string StudentName,
    string AdmissionNumber,
    string ClassName,
    string ScanType, // Boarding, DropOff
    Guid? StopId,
    string? StopName,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset ScannedAt,
    string Message);

/// <summary>
/// Event broadcast to tracking groups when a trip lifecycle state transitions.
/// </summary>
public sealed record TripStatusChangedDto(
    Guid TripId,
    Guid SchoolId,
    Guid RouteId,
    string RouteName,
    Guid BusId,
    string BusNumber,
    Guid DriverId,
    string DriverName,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset Timestamp,
    string? Notes);

/// <summary>
/// Event broadcast when a bus arrives at, departs from, or skips an ordered route stop.
/// </summary>
public sealed record TripStopStatusChangedDto(
    Guid TripId,
    Guid SchoolId,
    Guid StopId,
    string StopName,
    int SequenceOrder,
    string Status, // Arrived, Departed, Skipped
    DateTimeOffset Timestamp);

/// <summary>
/// Event broadcast when a bus goes offline or restores active telemetry transmission.
/// </summary>
public sealed record BusConnectivityChangedDto(
    Guid SchoolId,
    Guid BusId,
    string BusNumber,
    bool IsOnline,
    DateTimeOffset Timestamp);

