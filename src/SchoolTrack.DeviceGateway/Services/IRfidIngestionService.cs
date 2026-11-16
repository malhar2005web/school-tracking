using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Common.Services;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.DeviceGateway.Services;

public interface IRfidIngestionService
{
    Task<RfidScanResultDto> IngestTapAsync(RfidScanPacket packet, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfidScanResultDto>> IngestBatchTapsAsync(RfidBatchScanRequest request, CancellationToken cancellationToken = default);
}

public sealed class RfidIngestionService : IRfidIngestionService
{
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(15);

    private readonly IApplicationDbContext _context;
    private readonly IGeofenceEvaluator _geofenceEvaluator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrackingBroadcaster _broadcaster;
    private readonly ILogger<RfidIngestionService> _logger;

    public RfidIngestionService(
        IApplicationDbContext context,
        IGeofenceEvaluator geofenceEvaluator,
        IDateTimeProvider dateTimeProvider,
        ILogger<RfidIngestionService> logger,
        ITrackingBroadcaster? broadcaster = null)
    {
        _context = context;
        _geofenceEvaluator = geofenceEvaluator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _broadcaster = broadcaster ?? new NullTrackingBroadcaster();
    }

    public async Task<RfidScanResultDto> IngestTapAsync(RfidScanPacket packet, CancellationToken cancellationToken = default)
    {
        var scannedAt = packet.ScannedAt;
        var utcNow = _dateTimeProvider.UtcNow;

        // 1. Authenticate reader device
        var device = await AuthenticateDeviceAsync(packet.DeviceCode, packet.ApiKey, cancellationToken);
        if (device.BusId == null)
        {
            throw new InvalidOperationException($"RFID reader '{device.DeviceCode}' is not assigned to any bus.");
        }

        var busId = device.BusId.Value;
        var schoolId = device.SchoolId;

        // 2. Resolve active trip on this bus
        var activeTrip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Stops)
                .ThenInclude(ts => ts.RouteStop)
            .FirstOrDefaultAsync(t => t.BusId == busId && (t.Status == TripStatuses.Started || t.Status == TripStatuses.InTransit), cancellationToken);

        if (activeTrip == null)
        {
            await RecordUnknownTapAsync(schoolId, busId, null, device.Id, device.DeviceCode, packet.CardUid, "NoActiveTrip", packet.Latitude, packet.Longitude, scannedAt, cancellationToken);
            return new RfidScanResultDto(
                Success: false,
                Message: "No active trip is in progress on this bus.",
                ScanType: null,
                StudentId: null,
                StudentName: null,
                TripId: null,
                StopId: null,
                StopName: null,
                ScannedAt: scannedAt);
        }

        // 3. Resolve RFID card and linked student
        var normalizedUid = packet.CardUid.Trim().ToUpperInvariant();
        var card = await _context.RfidCards
            .IgnoreQueryFilters()
            .Include(c => c.Student)
            .FirstOrDefaultAsync(c => c.SchoolId == schoolId && c.CardUid == normalizedUid, cancellationToken);

        if (card == null)
        {
            await RecordUnknownTapAsync(schoolId, busId, activeTrip.Id, device.Id, device.DeviceCode, normalizedUid, "UnregisteredCard", packet.Latitude, packet.Longitude, scannedAt, cancellationToken);
            return new RfidScanResultDto(
                Success: false,
                Message: $"RFID card '{normalizedUid}' is not registered in this school.",
                ScanType: null,
                StudentId: null,
                StudentName: null,
                TripId: activeTrip.Id,
                StopId: null,
                StopName: null,
                ScannedAt: scannedAt);
        }

        if (card.StudentId == null || card.Student == null)
        {
            await RecordUnknownTapAsync(schoolId, busId, activeTrip.Id, device.Id, device.DeviceCode, normalizedUid, "UnassignedCard", packet.Latitude, packet.Longitude, scannedAt, cancellationToken);
            return new RfidScanResultDto(
                Success: false,
                Message: $"RFID card '{normalizedUid}' is registered but not assigned to any student.",
                ScanType: null,
                StudentId: null,
                StudentName: null,
                TripId: activeTrip.Id,
                StopId: null,
                StopName: null,
                ScannedAt: scannedAt);
        }

        var student = card.Student;

        // 4. Deduplication / Rapid double-tap protection
        var lastEvent = await _context.BoardingEvents
            .IgnoreQueryFilters()
            .Where(b => b.TripId == activeTrip.Id && b.StudentId == student.Id)
            .OrderByDescending(b => b.ScannedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastEvent != null && (scannedAt - lastEvent.ScannedAt) < DeduplicationWindow)
        {
            _logger.LogWarning("Duplicate scan ignored for student {StudentId} ({CardUid}) within cooldown window.", student.Id, normalizedUid);
            return new RfidScanResultDto(
                Success: false,
                Message: $"Duplicate scan ignored for {student.FullName} within 15-second cooldown.",
                ScanType: lastEvent.ScanType,
                StudentId: student.Id,
                StudentName: student.FullName,
                TripId: activeTrip.Id,
                StopId: lastEvent.RouteStopId,
                StopName: null,
                ScannedAt: scannedAt);
        }

        // 5. Intelligent state determination: Boarding vs Drop-Off
        string scanType = (lastEvent == null || lastEvent.ScanType == BoardingScanTypes.DropOff)
            ? BoardingScanTypes.Boarding
            : BoardingScanTypes.DropOff;

        // 6. Geographic coordinates & RouteStop proximity matching
        decimal? lat = packet.Latitude;
        decimal? lon = packet.Longitude;

        if (!lat.HasValue || !lon.HasValue)
        {
            var latestLocation = await _context.LatestBusLocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.BusId == busId, cancellationToken);

            if (latestLocation != null)
            {
                lat = latestLocation.Latitude;
                lon = latestLocation.Longitude;
            }
        }

        Guid? matchedStopId = null;
        string? matchedStopName = null;

        if (lat.HasValue && lon.HasValue)
        {
            var routeStops = activeTrip.Stops
                .Where(ts => ts.RouteStop != null)
                .Select(ts => ts.RouteStop!)
                .ToList();

            foreach (var stop in routeStops)
            {
                var eval = _geofenceEvaluator.Evaluate(lat.Value, lon.Value, stop.Latitude, stop.Longitude, stop.GeofenceRadiusMeters);
                if (eval.IsWithinGeofence)
                {
                    matchedStopId = stop.Id;
                    matchedStopName = stop.StopName;
                    break;
                }
            }
        }

        // 7. Persist BoardingEvent
        var boardingEvent = new BoardingEvent
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            TripId = activeTrip.Id,
            StudentId = student.Id,
            RfidCardId = card.Id,
            RouteStopId = matchedStopId,
            BusId = busId,
            RfidDeviceId = device.Id,
            ScanType = scanType,
            Latitude = lat,
            Longitude = lon,
            ScannedAt = scannedAt,
            RecordedAt = utcNow
        };
        _context.BoardingEvents.Add(boardingEvent);

        // 8. Update device health
        device.LastSeenAt = utcNow;
        device.LastScanAt = scannedAt;
        device.Status = "Online";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded {ScanType} event for Student {StudentId} ({FullName}) on Trip {TripId} at Stop '{StopName}'",
            scanType, student.Id, student.FullName, activeTrip.Id, matchedStopName ?? "In-Transit");

        // Find linked parents of this student
        var parentUserIds = await _context.ParentStudents
            .IgnoreQueryFilters()
            .Include(ps => ps.Parent)
            .Where(ps => ps.StudentId == student.Id && ps.Parent != null)
            .Select(ps => ps.Parent!.UserId)
            .ToListAsync(cancellationToken);

        var busNumber = device.Bus?.BusNumber ?? "Unknown";
        var routeName = activeTrip.Route?.Name ?? "Unknown";
        var stopLabel = matchedStopName != null ? $"at {matchedStopName}" : "in-transit";
        var action = scanType == BoardingScanTypes.Boarding ? "boarded" : "deboarded";
        var notificationMessage = $"{student.FullName} has {action} Bus {busNumber} {stopLabel}.";

        var notification = new BoardingNotificationDto(
            boardingEvent.Id,
            schoolId,
            activeTrip.Id,
            routeName,
            busId,
            busNumber,
            student.Id,
            student.FullName,
            student.AdmissionNumber,
            student.Class?.Name ?? "Unassigned",
            scanType,
            matchedStopId,
            matchedStopName,
            lat,
            lon,
            scannedAt,
            notificationMessage);

        await _broadcaster.BroadcastBoardingNotificationAsync(notification, parentUserIds, cancellationToken);

        return new RfidScanResultDto(
            Success: true,
            Message: $"Student {student.FullName} {scanType.ToLowerInvariant()} successfully recorded.",
            ScanType: scanType,
            StudentId: student.Id,
            StudentName: student.FullName,
            TripId: activeTrip.Id,
            StopId: matchedStopId,
            StopName: matchedStopName,
            ScannedAt: scannedAt);
    }

    public async Task<IReadOnlyList<RfidScanResultDto>> IngestBatchTapsAsync(RfidBatchScanRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<RfidScanResultDto>();

        // Sort batch scans chronologically
        var sortedScans = request.Scans
            .OrderBy(s => s.ScannedAt ?? _dateTimeProvider.UtcNow)
            .ToList();

        foreach (var scan in sortedScans)
        {
            var packet = new RfidScanPacket(
                request.DeviceCode,
                request.ApiKey,
                scan.CardUid,
                scan.Latitude,
                scan.Longitude,
                scan.ScannedAt ?? _dateTimeProvider.UtcNow);

            var result = await IngestTapAsync(packet, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<RfidDevice> AuthenticateDeviceAsync(string deviceCode, string? apiKey, CancellationToken cancellationToken)
    {
        var normalizedCode = deviceCode.Trim().ToUpperInvariant();

        var device = await _context.RfidDevices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.DeviceCode == normalizedCode, cancellationToken);

        if (device == null)
        {
            throw new UnauthorizedAccessException($"RFID device with code '{normalizedCode}' is not registered.");
        }

        if (!string.IsNullOrWhiteSpace(device.ApiKey) && !string.Equals(device.ApiKey, apiKey, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Invalid API key provided for RFID device '{normalizedCode}'.");
        }

        return device;
    }

    private async Task RecordUnknownTapAsync(
        Guid schoolId,
        Guid? busId,
        Guid? tripId,
        Guid? deviceId,
        string deviceCode,
        string cardUid,
        string reason,
        decimal? latitude,
        decimal? longitude,
        DateTimeOffset scannedAt,
        CancellationToken cancellationToken)
    {
        var unknownTap = new UnknownCardTap
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            BusId = busId,
            TripId = tripId,
            RfidDeviceId = deviceId,
            DeviceCode = deviceCode,
            CardUid = cardUid,
            Reason = reason,
            Latitude = latitude,
            Longitude = longitude,
            ScannedAt = scannedAt
        };

        _context.UnknownCardTaps.Add(unknownTap);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

