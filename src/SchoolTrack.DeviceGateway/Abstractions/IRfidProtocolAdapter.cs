namespace SchoolTrack.DeviceGateway.Abstractions;

/// <summary>
/// Normalized RFID card scan event produced by a hardware protocol adapter.
/// </summary>
public sealed record RfidScanPacket(
    string DeviceCode,
    string? ApiKey,
    string CardUid,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset ScannedAt,
    string? RawPayload = null);

/// <summary>
/// Hardware-agnostic protocol adapter for parsing vendor-specific RFID reader wire packets (HTTP/JSON, TCP, UDP, SDK).
/// </summary>
public interface IRfidProtocolAdapter
{
    string ProtocolName { get; }

    bool CanHandle(string protocolType);

    RfidScanPacket Parse(string rawPayload);
}

