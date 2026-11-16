using System.Text.Json;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Adapters;

/// <summary>
/// Reference protocol adapter for standard HTTP/JSON telematics packets and simulators.
/// </summary>
public sealed class HttpJsonGpsAdapter : IGpsProtocolAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProtocolName => "Simulator/HttpJson";

    public bool CanHandle(string protocolType)
    {
        return string.Equals(protocolType, "Simulator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "Http", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "HttpJson", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolType, "Json", StringComparison.OrdinalIgnoreCase);
    }

    public GpsTelemetryPacket Parse(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("GPS payload cannot be empty.", nameof(rawPayload));
        }

        var request = JsonSerializer.Deserialize<GpsTelemetryRequest>(rawPayload, JsonOptions)
            ?? throw new FormatException("Failed to deserialize GPS JSON payload.");

        return new GpsTelemetryPacket(
            request.DeviceCode,
            request.ApiKey,
            request.Imei,
            request.Latitude,
            request.Longitude,
            request.SpeedKmh,
            request.Heading,
            request.Altitude,
            request.AccuracyMeters,
            request.IgnitionOn,
            request.RecordedAt ?? DateTimeOffset.UtcNow,
            rawPayload);
    }
}

