using System.Text.Json;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Adapters;

/// <summary>
/// Reference protocol adapter for standard HTTP/JSON card scan packets and simulators.
/// </summary>
public sealed class HttpJsonRfidAdapter : IRfidProtocolAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProtocolName => "Simulator/HttpJsonRfid";

    public bool CanHandle(string protocolType)
    {
        return string.Equals(protocolType, "Simulator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "Http", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "HttpJson", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "Json", StringComparison.OrdinalIgnoreCase);
    }

    public RfidScanPacket Parse(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("RFID payload cannot be empty.", nameof(rawPayload));
        }

        var request = JsonSerializer.Deserialize<RfidScanRequest>(rawPayload, JsonOptions)
            ?? throw new FormatException("Failed to deserialize RFID JSON payload.");

        return new RfidScanPacket(
            request.DeviceCode,
            request.ApiKey,
            request.CardUid,
            request.Latitude,
            request.Longitude,
            request.ScannedAt ?? DateTimeOffset.UtcNow,
            rawPayload);
    }
}

