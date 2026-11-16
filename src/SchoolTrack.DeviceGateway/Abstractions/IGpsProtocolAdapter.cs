namespace SchoolTrack.DeviceGateway.Abstractions;

/// <summary>
/// Normalized telematics packet produced by a hardware protocol adapter.
/// </summary>
public sealed record GpsTelemetryPacket(
    string DeviceCode,
    string? ApiKey,
    string? Imei,
    decimal Latitude,
    decimal Longitude,
    decimal SpeedKmh,
    decimal Heading,
    decimal? Altitude,
    decimal? AccuracyMeters,
    bool IgnitionOn,
    DateTimeOffset RecordedAt,
    string? RawPayload);

/// <summary>
/// Hardware-agnostic protocol adapter for parsing vendor-specific GPS wire packets (HTTP/JSON, TCP, UDP, MQTT).
/// </summary>
public interface IGpsProtocolAdapter
{
    string ProtocolName { get; }

    bool CanHandle(string protocolType);

    GpsTelemetryPacket Parse(string rawPayload);
}

