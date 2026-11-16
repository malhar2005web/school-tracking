namespace SchoolTrack.Shared.DTOs.Devices;

/// <summary>
/// Ingestion payload sent by a GPS hardware device or simulator.
/// </summary>
public sealed record GpsTelemetryRequest(
    string DeviceCode,
    string? ApiKey,
    string? Imei,
    decimal Latitude,
    decimal Longitude,
    decimal SpeedKmh,
    decimal Heading,
    decimal? Altitude = null,
    decimal? AccuracyMeters = null,
    bool IgnitionOn = true,
    DateTimeOffset? RecordedAt = null,
    string? RawPayload = null);

/// <summary>
/// A single timestamped waypoint in a batch telemetry synchronization.
/// </summary>
public sealed record GpsTelemetryPointDto(
    decimal Latitude,
    decimal Longitude,
    decimal SpeedKmh,
    decimal Heading,
    decimal? Altitude = null,
    decimal? AccuracyMeters = null,
    bool IgnitionOn = true,
    DateTimeOffset? RecordedAt = null);

/// <summary>
/// Batch of queued telematics points transmitted when cellular connection is restored.
/// </summary>
public sealed record GpsBatchTelemetryRequest(
    string DeviceCode,
    string? ApiKey,
    string? Imei,
    IReadOnlyList<GpsTelemetryPointDto> Points);

/// <summary>
/// Fast projected latest coordinates, speed, and connectivity state of a bus.
/// </summary>
public sealed record LatestBusLocationDto(
    Guid BusId,
    string BusNumber,
    Guid SchoolId,
    Guid? TripId,
    string? TripStatus,
    decimal Latitude,
    decimal Longitude,
    decimal SpeedKmh,
    decimal Heading,
    decimal? Altitude,
    decimal? AccuracyMeters,
    bool IgnitionOn,
    bool IsOnline,
    DateTimeOffset RecordedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Historical telemetry point along a bus route trail.
/// </summary>
public sealed record LocationLogDto(
    Guid Id,
    Guid BusId,
    Guid? TripId,
    decimal Latitude,
    decimal Longitude,
    decimal SpeedKmh,
    decimal Heading,
    decimal? Altitude,
    decimal? AccuracyMeters,
    bool IgnitionOn,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt);

/// <summary>
/// Acknowledgement response returned after processing GPS telemetry.
/// </summary>
public sealed record GpsIngestionResultDto(
    bool Success,
    string Message,
    Guid? BusId,
    Guid? TripId,
    DateTimeOffset RecordedAt,
    bool IsNearStop,
    string? NearestStopName,
    double? DistanceToStopMeters);

/// <summary>
/// Trigger payload for executing the synthetic route telemetry simulator.
/// </summary>
public sealed record SimulateGpsTripRequest(
    Guid TripId,
    int StepSeconds = 5,
    decimal AverageSpeedKmh = 35m);

/// <summary>
/// Summary report returned after a route simulation run.
/// </summary>
public sealed record SimulateGpsTripResponse(
    Guid TripId,
    Guid BusId,
    string RouteName,
    int PointsGenerated,
    int PointsIngested,
    TimeSpan ElapsedSimulationTime);

