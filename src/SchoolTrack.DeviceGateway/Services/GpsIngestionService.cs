using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Common.Services;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Validation;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Services;

public interface IGpsIngestionService
{
    Task<GpsIngestionResultDto> IngestAsync(GpsTelemetryPacket packet, CancellationToken cancellationToken = default);
    Task<GpsIngestionResultDto> IngestBatchAsync(GpsBatchTelemetryRequest request, CancellationToken cancellationToken = default);
}

public sealed class GpsIngestionService : IGpsIngestionService
{
    private readonly IApplicationDbContext _context;
    private readonly IGpsValidationService _validationService;
    private readonly IGeofenceEvaluator _geofenceEvaluator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrackingBroadcaster _broadcaster;
    private readonly ILogger<GpsIngestionService> _logger;

    public GpsIngestionService(
        IApplicationDbContext context,
        IGpsValidationService validationService,
        IGeofenceEvaluator geofenceEvaluator,
        IDateTimeProvider dateTimeProvider,
        ILogger<GpsIngestionService> logger,
        ITrackingBroadcaster? broadcaster = null)
    {
        _context = context;
        _validationService = validationService;
        _geofenceEvaluator = geofenceEvaluator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _broadcaster = broadcaster ?? new NullTrackingBroadcaster();
    }

    public async Task<GpsIngestionResultDto> IngestAsync(GpsTelemetryPacket packet, CancellationToken cancellationToken = default)
    {
        // 1. Validate packet
        var validation = _validationService.Validate(packet);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Invalid GPS telemetry.");
        }

        // 2. Authenticate device and resolve bus
        var device = await AuthenticateDeviceAsync(packet.DeviceCode, packet.ApiKey, packet.Imei, cancellationToken);
        if (device.BusId == null)
        {
            throw new InvalidOperationException($"GPS device '{device.DeviceCode}' is not assigned to any bus.");
        }

        var busId = device.BusId.Value;
        var schoolId = device.SchoolId;
        var utcNow = _dateTimeProvider.UtcNow;

        // 3. Resolve active trip on this bus
        var activeTrip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Stops)
                .ThenInclude(ts => ts.RouteStop)
            .FirstOrDefaultAsync(t => t.BusId == busId && (t.Status == TripStatuses.Started || t.Status == TripStatuses.InTransit), cancellationToken);

        // 4. Evaluate geofence proximity against upcoming stops on active trip
        bool isNearStop = false;
        string? nearestStopName = null;
        double? minDistanceMeters = null;

        if (activeTrip != null)
        {
            var pendingStops = activeTrip.Stops
                .Where(ts => ts.Status == TripStopStatuses.Pending && ts.RouteStop != null)
                .OrderBy(ts => ts.SequenceOrder)
                .ToList();

            foreach (var stop in pendingStops)
            {
                var eval = _geofenceEvaluator.Evaluate(
                    packet.Latitude,
                    packet.Longitude,
                    stop.RouteStop!.Latitude,
                    stop.RouteStop.Longitude,
                    stop.RouteStop.GeofenceRadiusMeters);

                if (minDistanceMeters == null || eval.DistanceMeters < minDistanceMeters.Value)
                {
                    minDistanceMeters = eval.DistanceMeters;
                    nearestStopName = stop.RouteStop.StopName;
                }

                if (eval.IsWithinGeofence)
                {
                    isNearStop = true;
                    nearestStopName = stop.RouteStop.StopName;
                    minDistanceMeters = eval.DistanceMeters;
                    break;
                }
            }
        }

        // 5. Append historical telemetry record
        var locationLog = new LocationLog
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            BusId = busId,
            TripId = activeTrip?.Id,
            Latitude = packet.Latitude,
            Longitude = packet.Longitude,
            SpeedKmh = packet.SpeedKmh,
            Heading = packet.Heading,
            Altitude = packet.Altitude,
            AccuracyMeters = packet.AccuracyMeters,
            IgnitionOn = packet.IgnitionOn,
            RecordedAt = packet.RecordedAt,
            ReceivedAt = utcNow,
            RawPayload = packet.RawPayload
        };
        _context.LocationLogs.Add(locationLog);

        // 6. Upsert latest bus location projection
        var latestLocation = await _context.LatestBusLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.BusId == busId, cancellationToken);

        if (latestLocation == null)
        {
            latestLocation = new LatestBusLocation
            {
                Id = Guid.CreateVersion7(),
                SchoolId = schoolId,
                BusId = busId,
                TripId = activeTrip?.Id,
                Latitude = packet.Latitude,
                Longitude = packet.Longitude,
                SpeedKmh = packet.SpeedKmh,
                Heading = packet.Heading,
                Altitude = packet.Altitude,
                AccuracyMeters = packet.AccuracyMeters,
                IgnitionOn = packet.IgnitionOn,
                IsOnline = true,
                RecordedAt = packet.RecordedAt
            };
            _context.LatestBusLocations.Add(latestLocation);
        }
        else if (packet.RecordedAt >= latestLocation.RecordedAt)
        {
            latestLocation.TripId = activeTrip?.Id;
            latestLocation.Latitude = packet.Latitude;
            latestLocation.Longitude = packet.Longitude;
            latestLocation.SpeedKmh = packet.SpeedKmh;
            latestLocation.Heading = packet.Heading;
            latestLocation.Altitude = packet.Altitude;
            latestLocation.AccuracyMeters = packet.AccuracyMeters;
            latestLocation.IgnitionOn = packet.IgnitionOn;
            latestLocation.IsOnline = true;
            latestLocation.RecordedAt = packet.RecordedAt;
        }

        // 7. Update device metadata
        device.LastSeenAt = utcNow;
        device.LastLocationAt = packet.RecordedAt;
        device.Status = "Online";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ingested GPS telemetry for Bus {BusId} at {Latitude},{Longitude} (Speed: {Speed} km/h, ActiveTrip: {TripId})",
            busId, packet.Latitude, packet.Longitude, packet.SpeedKmh, activeTrip?.Id);

        var busNumber = device.Bus?.BusNumber ?? "Unknown";
        var locationDto = new LatestBusLocationDto(
            busId,
            busNumber,
            schoolId,
            activeTrip?.Id,
            activeTrip?.Status,
            packet.Latitude,
            packet.Longitude,
            packet.SpeedKmh,
            packet.Heading,
            packet.Altitude,
            packet.AccuracyMeters,
            packet.IgnitionOn,
            true,
            packet.RecordedAt,
            utcNow);

        await _broadcaster.BroadcastBusLocationAsync(locationDto, cancellationToken);

        return new GpsIngestionResultDto(
            Success: true,
            Message: "GPS telemetry successfully ingested.",
            BusId: busId,
            TripId: activeTrip?.Id,
            RecordedAt: packet.RecordedAt,
            IsNearStop: isNearStop,
            NearestStopName: nearestStopName,
            DistanceToStopMeters: minDistanceMeters);
    }

    public async Task<GpsIngestionResultDto> IngestBatchAsync(GpsBatchTelemetryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Points == null || request.Points.Count == 0)
        {
            throw new ArgumentException("Batch telemetry must contain at least one point.", nameof(request));
        }

        var device = await AuthenticateDeviceAsync(request.DeviceCode, request.ApiKey, request.Imei, cancellationToken);
        if (device.BusId == null)
        {
            throw new InvalidOperationException($"GPS device '{device.DeviceCode}' is not assigned to any bus.");
        }

        var busId = device.BusId.Value;
        var schoolId = device.SchoolId;
        var utcNow = _dateTimeProvider.UtcNow;

        var activeTrip = await _context.Trips
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.BusId == busId && (t.Status == TripStatuses.Started || t.Status == TripStatuses.InTransit), cancellationToken);

        // Sort batch points chronologically
        var sortedPoints = request.Points
            .OrderBy(p => p.RecordedAt ?? utcNow)
            .ToList();

        GpsTelemetryPointDto? latestValidPoint = null;

        foreach (var point in sortedPoints)
        {
            var recordedAt = point.RecordedAt ?? utcNow;
            var validation = _validationService.Validate(point.Latitude, point.Longitude, point.SpeedKmh, point.Heading, recordedAt);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Skipping invalid batch point ({Lat},{Lon}): {Reason}", point.Latitude, point.Longitude, validation.ErrorMessage);
                continue;
            }

            var locationLog = new LocationLog
            {
                Id = Guid.CreateVersion7(),
                SchoolId = schoolId,
                BusId = busId,
                TripId = activeTrip?.Id,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                SpeedKmh = point.SpeedKmh,
                Heading = point.Heading,
                Altitude = point.Altitude,
                AccuracyMeters = point.AccuracyMeters,
                IgnitionOn = point.IgnitionOn,
                RecordedAt = recordedAt,
                ReceivedAt = utcNow
            };

            _context.LocationLogs.Add(locationLog);
            latestValidPoint = point;
        }

        if (latestValidPoint != null)
        {
            var recordedAt = latestValidPoint.RecordedAt ?? utcNow;
            var latestLocation = await _context.LatestBusLocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.BusId == busId, cancellationToken);

            if (latestLocation == null)
            {
                latestLocation = new LatestBusLocation
                {
                    Id = Guid.CreateVersion7(),
                    SchoolId = schoolId,
                    BusId = busId,
                    TripId = activeTrip?.Id,
                    Latitude = latestValidPoint.Latitude,
                    Longitude = latestValidPoint.Longitude,
                    SpeedKmh = latestValidPoint.SpeedKmh,
                    Heading = latestValidPoint.Heading,
                    Altitude = latestValidPoint.Altitude,
                    AccuracyMeters = latestValidPoint.AccuracyMeters,
                    IgnitionOn = latestValidPoint.IgnitionOn,
                    IsOnline = true,
                    RecordedAt = recordedAt
                };
                _context.LatestBusLocations.Add(latestLocation);
            }
            else if (recordedAt >= latestLocation.RecordedAt)
            {
                latestLocation.TripId = activeTrip?.Id;
                latestLocation.Latitude = latestValidPoint.Latitude;
                latestLocation.Longitude = latestValidPoint.Longitude;
                latestLocation.SpeedKmh = latestValidPoint.SpeedKmh;
                latestLocation.Heading = latestValidPoint.Heading;
                latestLocation.Altitude = latestValidPoint.Altitude;
                latestLocation.AccuracyMeters = latestValidPoint.AccuracyMeters;
                latestLocation.IgnitionOn = latestValidPoint.IgnitionOn;
                latestLocation.IsOnline = true;
                latestLocation.RecordedAt = recordedAt;
            }

            device.LastSeenAt = utcNow;
            device.LastLocationAt = recordedAt;
            device.Status = "Online";
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ingested batch of {Count} GPS points for Bus {BusId}", sortedPoints.Count, busId);

        return new GpsIngestionResultDto(
            Success: true,
            Message: $"Batch of {sortedPoints.Count} telemetry points processed.",
            BusId: busId,
            TripId: activeTrip?.Id,
            RecordedAt: latestValidPoint?.RecordedAt ?? utcNow,
            IsNearStop: false,
            NearestStopName: null,
            DistanceToStopMeters: null);
    }

    private async Task<GpsDevice> AuthenticateDeviceAsync(
        string deviceCode,
        string? apiKey,
        string? imei,
        CancellationToken cancellationToken)
    {
        var normalizedCode = deviceCode.Trim().ToUpperInvariant();

        var device = await _context.GpsDevices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.DeviceCode == normalizedCode, cancellationToken);

        if (device == null)
        {
            throw new UnauthorizedAccessException($"GPS device with code '{normalizedCode}' is not registered.");
        }

        if (!string.IsNullOrWhiteSpace(device.ApiKey) && !string.Equals(device.ApiKey, apiKey, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Invalid API key provided for GPS device '{normalizedCode}'.");
        }

        if (!string.IsNullOrWhiteSpace(imei) && !string.IsNullOrWhiteSpace(device.Imei) &&
            !string.Equals(device.Imei, imei.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"IMEI mismatch for GPS device '{normalizedCode}'.");
        }

        return device;
    }
}

