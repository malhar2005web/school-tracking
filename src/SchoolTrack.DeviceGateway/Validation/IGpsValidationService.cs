using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.DeviceGateway.Validation;

public sealed record GpsValidationResult(
    bool IsValid,
    string? ErrorMessage = null);

public interface IGpsValidationService
{
    GpsValidationResult Validate(GpsTelemetryPacket packet);
    GpsValidationResult Validate(decimal latitude, decimal longitude, decimal speedKmh, decimal heading, DateTimeOffset recordedAt);
}

public sealed class GpsValidationService : IGpsValidationService
{
    private static readonly TimeSpan MaxFutureClockSkew = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxHistoricalAge = TimeSpan.FromDays(30);

    private readonly IDateTimeProvider _dateTimeProvider;

    public GpsValidationService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public GpsValidationResult Validate(GpsTelemetryPacket packet)
    {
        if (string.IsNullOrWhiteSpace(packet.DeviceCode))
        {
            return new GpsValidationResult(false, "DeviceCode is required.");
        }

        return Validate(packet.Latitude, packet.Longitude, packet.SpeedKmh, packet.Heading, packet.RecordedAt);
    }

    public GpsValidationResult Validate(
        decimal latitude,
        decimal longitude,
        decimal speedKmh,
        decimal heading,
        DateTimeOffset recordedAt)
    {
        if (latitude < -90.0m || latitude > 90.0m)
        {
            return new GpsValidationResult(false, $"Latitude '{latitude}' is invalid. Must be between -90 and +90.");
        }

        if (longitude < -180.0m || longitude > 180.0m)
        {
            return new GpsValidationResult(false, $"Longitude '{longitude}' is invalid. Must be between -180 and +180.");
        }

        // Check for unresolved GPS lock at 0,0
        if (latitude == 0m && longitude == 0m)
        {
            return new GpsValidationResult(false, "Coordinates (0,0) indicate an unresolved GPS fix.");
        }

        if (speedKmh < 0m || speedKmh > 200m)
        {
            return new GpsValidationResult(false, $"Speed '{speedKmh}' km/h is out of realistic physical bounds [0, 200].");
        }

        if (heading < 0m || heading > 360m)
        {
            return new GpsValidationResult(false, $"Heading '{heading}' degrees is out of valid bounds [0, 360].");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        if (recordedAt > utcNow + MaxFutureClockSkew)
        {
            return new GpsValidationResult(false, $"Recorded timestamp '{recordedAt}' is in the future beyond acceptable clock skew (5 minutes).");
        }

        if (recordedAt < utcNow - MaxHistoricalAge)
        {
            return new GpsValidationResult(false, $"Recorded timestamp '{recordedAt}' exceeds maximum allowable historical age (30 days).");
        }

        return new GpsValidationResult(true);
    }
}

