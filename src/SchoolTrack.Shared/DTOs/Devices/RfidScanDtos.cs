namespace SchoolTrack.Shared.DTOs.Devices;

/// <summary>
/// Telematics card tap payload sent by an on-bus Ethernet/cellular RFID reader.
/// </summary>
public sealed record RfidScanRequest(
    string DeviceCode,
    string? ApiKey,
    string CardUid,
    decimal? Latitude = null,
    decimal? Longitude = null,
    DateTimeOffset? ScannedAt = null);

/// <summary>
/// A single timestamped RFID tap in an offline queue sync.
/// </summary>
public sealed record RfidScanPointDto(
    string CardUid,
    decimal? Latitude = null,
    decimal? Longitude = null,
    DateTimeOffset? ScannedAt = null);

/// <summary>
/// Batch of queued card taps transmitted after network reconnection.
/// </summary>
public sealed record RfidBatchScanRequest(
    string DeviceCode,
    string? ApiKey,
    IReadOnlyList<RfidScanPointDto> Scans);

/// <summary>
/// Result returned after processing an RFID card tap.
/// </summary>
public sealed record RfidScanResultDto(
    bool Success,
    string Message,
    string? ScanType,
    Guid? StudentId,
    string? StudentName,
    Guid? TripId,
    Guid? StopId,
    string? StopName,
    DateTimeOffset ScannedAt);

/// <summary>
/// Payload to simulate a student RFID card tap.
/// </summary>
public sealed record SimulateRfidTapRequest(
    Guid TripId,
    Guid StudentId,
    Guid? StopId = null,
    string? OverrideScanType = null);

/// <summary>
/// Live passenger boarding manifest for an active or completed trip.
/// </summary>
public sealed record TripPassengerManifestDto(
    Guid TripId,
    string RouteName,
    string BusNumber,
    string DriverName,
    int TotalStudents,
    int BoardedCount,
    int DroppedOffCount,
    int PendingCount,
    IReadOnlyList<PassengerManifestItemDto> Passengers);

/// <summary>
/// Individual student record within a trip passenger manifest.
/// </summary>
public sealed record PassengerManifestItemDto(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    string ClassName,
    string? RfidCardUid,
    string CurrentStatus, // NotBoarded, Boarded, DroppedOff
    DateTimeOffset? BoardedAt,
    string? BoardedStopName,
    DateTimeOffset? DroppedOffAt,
    string? DroppedOffStopName);

/// <summary>
/// Audit log entry for unrecognized or unassigned RFID card scans.
/// </summary>
public sealed record UnknownCardTapDto(
    Guid Id,
    Guid SchoolId,
    string DeviceCode,
    string CardUid,
    string Reason,
    Guid? BusId,
    Guid? TripId,
    DateTimeOffset ScannedAt);

