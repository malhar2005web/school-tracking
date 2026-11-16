using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Features.Telemetry;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Simulator;
using SchoolTrack.DeviceGateway.Validation;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies hardware-agnostic GPS packet validation, Haversine geofencing, device authentication,
/// single & batch telemetry ingestion, latest location projection, and route simulation.
/// </summary>
public sealed class GpsGatewayTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new SystemDateTimeProvider();

    private SchoolTrackDbContext CreateDbContext(string dbName, Guid? schoolId = null, bool isPlatformAdmin = false)
    {
        var options = new DbContextOptionsBuilder<SchoolTrackDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantContext = new TestTenant(schoolId, isPlatformAdmin);
        return new SchoolTrackDbContext(options, tenantContext, _dateTimeProvider);
    }

    private static (Guid schoolId, Guid busId, Guid deviceId) SeedSchoolBusAndGpsDevice(
        SchoolTrackDbContext context,
        string deviceCode = "GPS-UNIT-01",
        string apiKey = "SecretKey123")
    {
        var schoolId = Guid.NewGuid();

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-GPS-01",
            RegistrationNumber = $"MH12GPS{Guid.NewGuid():N}"[..12],
            Capacity = 35,
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        var gpsDevice = new GpsDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = deviceCode,
            ApiKey = apiKey,
            Imei = "860123456789012",
            ProtocolType = "Simulator",
            Status = "Provisioning"
        };
        context.GpsDevices.Add(gpsDevice);

        context.SaveChanges();
        return (schoolId, bus.Id, gpsDevice.Id);
    }

    [Fact]
    public void GpsValidation_CoordinatesAndPhysicalBounds_ValidatedStrictly()
    {
        var validationService = new GpsValidationService(_dateTimeProvider);
        var now = _dateTimeProvider.UtcNow;

        // Valid point
        var valid = validationService.Validate(18.5204m, 73.8567m, 35m, 180m, now);
        Assert.True(valid.IsValid);
        Assert.Null(valid.ErrorMessage);

        // Invalid Latitude
        Assert.False(validationService.Validate(-91m, 73.8567m, 35m, 180m, now).IsValid);
        Assert.False(validationService.Validate(91m, 73.8567m, 35m, 180m, now).IsValid);

        // Invalid Longitude
        Assert.False(validationService.Validate(18.5204m, -181m, 35m, 180m, now).IsValid);
        Assert.False(validationService.Validate(18.5204m, 181m, 35m, 180m, now).IsValid);

        // Unresolved (0,0) fix
        Assert.False(validationService.Validate(0m, 0m, 0m, 0m, now).IsValid);

        // Unrealistic Speed (> 200 km/h)
        Assert.False(validationService.Validate(18.5204m, 73.8567m, 210m, 180m, now).IsValid);
        Assert.False(validationService.Validate(18.5204m, 73.8567m, -5m, 180m, now).IsValid);

        // Invalid Heading (> 360)
        Assert.False(validationService.Validate(18.5204m, 73.8567m, 35m, 365m, now).IsValid);

        // Future clock skew beyond 5 minutes
        Assert.False(validationService.Validate(18.5204m, 73.8567m, 35m, 180m, now.AddMinutes(10)).IsValid);

        // Stale timestamp beyond 30 days
        Assert.False(validationService.Validate(18.5204m, 73.8567m, 35m, 180m, now.AddDays(-35)).IsValid);
    }

    [Fact]
    public void HaversineDistance_AccurateSphericalCalculation()
    {
        // Point A: Shaniwar Wada, Pune (18.5196, 73.8553)
        // Point B: Pune Railway Station (18.5284, 73.8744)
        // Distance is ~2.24 km
        var distMeters = HaversineDistanceCalculator.CalculateDistanceMeters(
            18.5196, 73.8553, 18.5284, 73.8744);

        Assert.InRange(distMeters, 2100.0, 2400.0);

        // Same point distance is 0
        var zeroDist = HaversineDistanceCalculator.CalculateDistanceMeters(
            18.5196, 73.8553, 18.5196, 73.8553);
        Assert.Equal(0.0, zeroDist, precision: 3);

        // Heading due North: ~0 degrees
        var headingNorth = HaversineDistanceCalculator.CalculateHeadingDegrees(
            18.5000, 73.8500, 18.5100, 73.8500);
        Assert.InRange(headingNorth, 0.0, 1.0);
    }

    [Fact]
    public void GeofenceEvaluator_StopProximityDetection()
    {
        var evaluator = new GeofenceEvaluator();

        // Bus is ~15 meters away from stop with 50m radius -> INSIDE
        var closeEval = evaluator.Evaluate(
            18.520430m, 73.856744m,
            18.520550m, 73.856744m,
            50);

        Assert.True(closeEval.IsWithinGeofence);
        Assert.True(closeEval.DistanceMeters < 20.0);

        // Bus is ~1.1 km away -> OUTSIDE
        var farEval = evaluator.Evaluate(
            18.520430m, 73.856744m,
            18.530430m, 73.856744m,
            50);

        Assert.False(farEval.IsWithinGeofence);
        Assert.True(farEval.DistanceMeters > 1000.0);
    }

    [Fact]
    public async Task GpsIngestion_ValidPacket_UpdatesLatestBusLocationAndAppendsLog()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, _) = SeedSchoolBusAndGpsDevice(context, "GPS-DEV-01", "Key123");

        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance);

        var packetTime = _dateTimeProvider.UtcNow.AddSeconds(-10);
        var packet = new GpsTelemetryPacket(
            "GPS-DEV-01",
            "Key123",
            "860123456789012",
            18.520400m,
            73.856700m,
            32.5m,
            185.0m,
            120m,
            2.5m,
            true,
            packetTime,
            "{\"raw\":1}");

        var result = await ingestionService.IngestAsync(packet);

        Assert.True(result.Success);
        Assert.Equal(busId, result.BusId);

        // 1. Verify LocationLog was persisted
        var log = await context.LocationLogs.FirstOrDefaultAsync(l => l.BusId == busId);
        Assert.NotNull(log);
        Assert.Equal(18.520400m, log.Latitude);
        Assert.Equal(73.856700m, log.Longitude);
        Assert.Equal(32.5m, log.SpeedKmh);
        Assert.Equal(185.0m, log.Heading);
        Assert.Equal(packetTime, log.RecordedAt);

        // 2. Verify LatestBusLocation projection was updated
        var latest = await context.LatestBusLocations.FirstOrDefaultAsync(l => l.BusId == busId);
        Assert.NotNull(latest);
        Assert.Equal(18.520400m, latest.Latitude);
        Assert.Equal(73.856700m, latest.Longitude);
        Assert.True(latest.IsOnline);

        // 3. Verify GpsDevice status is Online
        var device = await context.GpsDevices.FirstOrDefaultAsync(d => d.DeviceCode == "GPS-DEV-01");
        Assert.NotNull(device);
        Assert.Equal("Online", device.Status);
        Assert.NotNull(device.LastLocationAt);
    }

    [Fact]
    public async Task GpsIngestion_AuthenticationFailure_ThrowsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        SeedSchoolBusAndGpsDevice(context, "GPS-DEV-AUTH", "CorrectSecret");

        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance);

        // Unregistered device code
        var unknownPacket = new GpsTelemetryPacket(
            "GPS-UNKNOWN", "CorrectSecret", null, 18.52m, 73.85m, 20m, 90m, null, null, true, _dateTimeProvider.UtcNow, null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ingestionService.IngestAsync(unknownPacket));

        // Wrong ApiKey
        var wrongKeyPacket = new GpsTelemetryPacket(
            "GPS-DEV-AUTH", "WrongSecret", null, 18.52m, 73.85m, 20m, 90m, null, null, true, _dateTimeProvider.UtcNow, null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ingestionService.IngestAsync(wrongKeyPacket));
    }

    [Fact]
    public async Task GpsIngestion_WithActiveTrip_AssociatesTripIdAndDetectsStopArrival()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, _) = SeedSchoolBusAndGpsDevice(context, "GPS-DEV-TRIP", "KeyTrip");

        // Seed Route and RouteStops
        var route = new Route
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Code = "R-GPS",
            Name = "GPS Test Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);

        var stop1 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "First Gate Stop",
            SequenceOrder = 1,
            Latitude = 18.520400m,
            Longitude = 73.856700m,
            GeofenceRadiusMeters = 50
        };
        var stop2 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Campus Main Stop",
            SequenceOrder = 2,
            Latitude = 18.540000m,
            Longitude = 73.870000m,
            GeofenceRadiusMeters = 50
        };
        context.RouteStops.AddRange(stop1, stop2);

        // Seed active Trip
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            BusId = busId,
            DriverId = Guid.NewGuid(),
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.Started,
            ActualStartAt = _dateTimeProvider.UtcNow.AddMinutes(-5)
        };
        context.Trips.Add(trip);

        var tripStop1 = new TripStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TripId = trip.Id,
            RouteStopId = stop1.Id,
            SequenceOrder = 1,
            Status = TripStopStatuses.Pending
        };
        var tripStop2 = new TripStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TripId = trip.Id,
            RouteStopId = stop2.Id,
            SequenceOrder = 2,
            Status = TripStopStatuses.Pending
        };
        context.TripStops.AddRange(tripStop1, tripStop2);
        await context.SaveChangesAsync();

        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance);

        // Ingest telemetry directly at Stop 1 coordinates
        var packet = new GpsTelemetryPacket(
            "GPS-DEV-TRIP",
            "KeyTrip",
            null,
            18.520410m,
            73.856705m,
            5m,
            45m,
            100m,
            2m,
            true,
            _dateTimeProvider.UtcNow,
            null);

        var result = await ingestionService.IngestAsync(packet);

        Assert.True(result.Success);
        Assert.Equal(trip.Id, result.TripId);
        Assert.True(result.IsNearStop);
        Assert.Equal("First Gate Stop", result.NearestStopName);
        Assert.NotNull(result.DistanceToStopMeters);
        Assert.True(result.DistanceToStopMeters < 20.0);

        // Verify LocationLog has tripId attached
        var log = await context.LocationLogs.FirstAsync(l => l.BusId == busId);
        Assert.Equal(trip.Id, log.TripId);

        // Verify LatestBusLocation has tripId attached
        var latest = await context.LatestBusLocations.FirstAsync(l => l.BusId == busId);
        Assert.Equal(trip.Id, latest.TripId);
    }

    [Fact]
    public async Task BatchIngestion_PreservesChronology_LatestLocationMatchesNewestPoint()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, _) = SeedSchoolBusAndGpsDevice(context, "GPS-DEV-BATCH", "BatchKey");

        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance);

        var t0 = _dateTimeProvider.UtcNow.AddMinutes(-2);
        var t1 = _dateTimeProvider.UtcNow.AddMinutes(-1);
        var t2 = _dateTimeProvider.UtcNow;

        var batchRequest = new GpsBatchTelemetryRequest(
            "GPS-DEV-BATCH",
            "BatchKey",
            null,
            new List<GpsTelemetryPointDto>
            {
                new(18.5100m, 73.8400m, 25m, 90m, RecordedAt: t0),
                new(18.5200m, 73.8500m, 35m, 95m, RecordedAt: t1),
                new(18.5300m, 73.8600m, 40m, 100m, RecordedAt: t2)
            });

        var result = await ingestionService.IngestBatchAsync(batchRequest);

        Assert.True(result.Success);

        // 3 LocationLogs created
        var logs = await context.LocationLogs.Where(l => l.BusId == busId).OrderBy(l => l.RecordedAt).ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.Equal(18.5100m, logs[0].Latitude);
        Assert.Equal(18.5200m, logs[1].Latitude);
        Assert.Equal(18.5300m, logs[2].Latitude);

        // Latest location must match the newest point (t2)
        var latest = await context.LatestBusLocations.FirstAsync(l => l.BusId == busId);
        Assert.Equal(18.5300m, latest.Latitude);
        Assert.Equal(73.8600m, latest.Longitude);
        Assert.Equal(40m, latest.SpeedKmh);
        Assert.Equal(t2, latest.RecordedAt);
    }

    [Fact]
    public async Task GpsRouteSimulator_SimulatesFullRouteWaypoints_IngestsSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, _) = SeedSchoolBusAndGpsDevice(context, "GPS-DEV-SIM", "SimKey");

        // Seed Route with 3 stops
        var route = new Route
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Code = "R-SIM",
            Name = "Simulation Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);

        var stop1 = new RouteStop { Id = Guid.NewGuid(), SchoolId = schoolId, RouteId = route.Id, StopName = "Origin", SequenceOrder = 1, Latitude = 18.5000m, Longitude = 73.8000m };
        var stop2 = new RouteStop { Id = Guid.NewGuid(), SchoolId = schoolId, RouteId = route.Id, StopName = "Midway", SequenceOrder = 2, Latitude = 18.5100m, Longitude = 73.8100m };
        var stop3 = new RouteStop { Id = Guid.NewGuid(), SchoolId = schoolId, RouteId = route.Id, StopName = "Destination", SequenceOrder = 3, Latitude = 18.5200m, Longitude = 73.8200m };
        context.RouteStops.AddRange(stop1, stop2, stop3);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            BusId = busId,
            DriverId = Guid.NewGuid(),
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.InTransit,
            ActualStartAt = _dateTimeProvider.UtcNow.AddMinutes(-10)
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance);

        var simulatorService = new GpsSimulatorService(
            context,
            ingestionService,
            _dateTimeProvider,
            NullLogger<GpsSimulatorService>.Instance);

        // Run simulation
        var simResult = await simulatorService.SimulateTripAsync(new SimulateGpsTripRequest(trip.Id, StepSeconds: 10, AverageSpeedKmh: 40m));

        Assert.Equal(trip.Id, simResult.TripId);
        Assert.True(simResult.PointsIngested > 0);

        // Verify trail query
        var trailHandler = new GetTripTrailQueryHandler(context);
        var trail = await trailHandler.HandleAsync(new GetTripTrailQuery(trip.Id));

        Assert.NotEmpty(trail);
        Assert.Equal(simResult.PointsIngested, trail.Count);
        // Verify chronological order
        Assert.True(trail.First().RecordedAt <= trail.Last().RecordedAt);
    }

    [Fact]
    public async Task LatestBusLocationQuery_ComputesOnlineStatusBasedOnHeartbeatThreshold()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        await using var context = CreateDbContext(dbName, schoolId);

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-STATUS-01",
            RegistrationNumber = "MH12STAT01",
            Capacity = 30,
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        // Fresh telemetry (30 seconds ago) -> Online
        var freshLocation = new LatestBusLocation
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            Latitude = 18.52m,
            Longitude = 73.85m,
            SpeedKmh = 20m,
            Heading = 180m,
            IsOnline = true,
            RecordedAt = _dateTimeProvider.UtcNow.AddSeconds(-30)
        };
        context.LatestBusLocations.Add(freshLocation);
        await context.SaveChangesAsync();

        var queryHandler = new GetLatestBusLocationQueryHandler(context, _dateTimeProvider);
        var freshDto = await queryHandler.HandleAsync(new GetLatestBusLocationQuery(bus.Id));
        Assert.True(freshDto.IsOnline);

        // Telemetry stale (3 minutes ago > 90 seconds threshold) -> Offline
        freshLocation.RecordedAt = _dateTimeProvider.UtcNow.AddMinutes(-3);
        await context.SaveChangesAsync();

        var staleDto = await queryHandler.HandleAsync(new GetLatestBusLocationQuery(bus.Id));
        Assert.False(staleDto.IsOnline);
    }
}

