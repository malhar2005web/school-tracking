using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Application.Common.Interfaces;
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
using Xunit;

namespace SchoolTrack.Api.Tests;

public sealed class EndToEndHardwareIntegrationTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new SystemDateTimeProvider();

    private SchoolTrackDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SchoolTrackDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantContext = new TestTenant(null, true);
        return new SchoolTrackDbContext(options, tenantContext, _dateTimeProvider);
    }

    private static (Guid SchoolId, Guid BusId, Guid TripId, Guid StudentId, string GpsCode, string GpsKey, string RfidCode, string RfidKey, string CardUid)
        SeedFullHardwareEnvironment(SchoolTrackDbContext context)
    {
        var schoolId = Guid.NewGuid();

        // 1. Bus
        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-HW-14",
            RegistrationNumber = "CA-SCH-9014",
            Capacity = 48,
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        // 2. Hardware Devices
        var gpsDevice = new GpsDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = "GPS-HW-14",
            ApiKey = "gps-secret-hw-14",
            Imei = "860192837465102",
            ProtocolType = "HttpJson",
            Status = "Active"
        };
        context.GpsDevices.Add(gpsDevice);

        var rfidDevice = new RfidDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = "RFID-HW-14",
            ApiKey = "rfid-secret-hw-14",
            ProtocolType = "EthernetWiegand",
            Status = "Active"
        };
        context.RfidDevices.Add(rfidDevice);

        // 3. User & Driver
        var user = new User
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FullName = "Robert Jenkins",
            Email = $"driver.{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashedpassword",
            Role = "Driver",
            IsActive = true
        };
        context.Users.Add(user);

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = user.Id,
            LicenseNumber = "CDL-90824-A",
            Phone = "+15553298472",
            AvailabilityStatus = "OnTrip"
        };
        context.Drivers.Add(driver);

        // 4. Class
        var cls = new Class
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Grade = "Grade 3",
            Division = "B",
            AcademicYear = "2026-2027"
        };
        context.Classes.Add(cls);

        // 5. Route & Ordered Stops
        var route = new Route
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = "Route 14-AM • Oakridge East",
            Code = "R14-AM",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);

        var stop1 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Willow Creek Estates",
            SequenceOrder = 1,
            Latitude = 37.7680m,
            Longitude = -122.4260m,
            GeofenceRadiusMeters = 35
        };
        var stop2 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Oakridge & 5th Ave",
            SequenceOrder = 2,
            Latitude = 37.7735m,
            Longitude = -122.4205m,
            GeofenceRadiusMeters = 35
        };
        var stop3 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Oakridge Elementary Campus",
            SequenceOrder = 3,
            Latitude = 37.7795m,
            Longitude = -122.4140m,
            GeofenceRadiusMeters = 50
        };
        context.RouteStops.AddRange(stop1, stop2, stop3);

        // 6. Active Trip
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            BusId = bus.Id,
            DriverId = driver.Id,
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = TripStatuses.Started,
            ActualStartAt = DateTimeOffset.UtcNow.AddMinutes(-20)
        };
        context.Trips.Add(trip);

        var tripStop1 = new TripStop
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            RouteStopId = stop1.Id,
            SequenceOrder = 1,
            Status = TripStopStatuses.Departed,
            ActualArrivalAt = DateTimeOffset.UtcNow.AddMinutes(-18),
            ActualDepartureAt = DateTimeOffset.UtcNow.AddMinutes(-17)
        };
        var tripStop2 = new TripStop
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            RouteStopId = stop2.Id,
            SequenceOrder = 2,
            Status = TripStopStatuses.Pending
        };
        var tripStop3 = new TripStop
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            RouteStopId = stop3.Id,
            SequenceOrder = 3,
            Status = TripStopStatuses.Pending
        };
        context.TripStops.AddRange(tripStop1, tripStop2, tripStop3);

        // 7. Student & Active RFID Card
        var student = new Student
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = cls.Id,
            FirstName = "Leo",
            LastName = "Martinez",
            AdmissionNumber = "ADM-2026-084",
            AssignedRouteId = route.Id,
            AssignedStopId = stop2.Id,
            IsActive = true
        };
        context.Students.Add(student);

        var rfidCard = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            CardUid = "4B-99-F2-10",
            CardType = "StudentCard",
            StudentId = student.Id,
            Status = "Active",
            IssuedAt = DateTimeOffset.UtcNow.AddMonths(-2)
        };
        context.RfidCards.Add(rfidCard);

        context.SaveChanges();

        return (schoolId, bus.Id, trip.Id, student.Id, gpsDevice.DeviceCode, gpsDevice.ApiKey, rfidDevice.DeviceCode, rfidDevice.ApiKey, rfidCard.CardUid);
    }

    [Fact]
    public async Task AdvancedGpsSimulator_KinematicTrajectory_GeneratesInterpolatedPacketsAndDwellsAtStops()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedGpsSimulator_KinematicTrajectory_GeneratesInterpolatedPacketsAndDwellsAtStops));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var validationService = new GpsValidationService(_dateTimeProvider);
        var gpsIngestion = new GpsIngestionService(context, validationService, geofenceEvaluator, _dateTimeProvider, NullLogger<GpsIngestionService>.Instance);
        var simulator = new AdvancedGpsSimulator(context, gpsIngestion, _dateTimeProvider, NullLogger<AdvancedGpsSimulator>.Instance);

        var options = new AdvancedGpsSimulationOptions(
            TripId: env.TripId,
            CruiseSpeedKmh: 35m,
            StepSeconds: 5,
            StopDwellSeconds: 15,
            SimulateOfflineDisruption: false,
            SimulateDegradedGps: false
        );

        // Act
        var result = await simulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(env.TripId, result.TripId);
        Assert.Equal(env.BusId, result.BusId);
        Assert.True(result.TotalWaypointsGenerated > 0);
        Assert.Equal(result.TotalWaypointsGenerated, result.TotalPacketsIngested);
        Assert.Equal(3, result.GeofenceArrivalTriggers); // 3 stops
        Assert.Contains(result.LogEvents, l => l.Contains("[STOP_ARRIVED]") && l.Contains("Oakridge & 5th Ave"));
        Assert.Contains(result.LogEvents, l => l.Contains("[SIM_COMPLETE]"));

        // Verify latest location projected on bus
        var latestLocation = await context.LatestBusLocations.FirstOrDefaultAsync(l => l.BusId == env.BusId);
        Assert.NotNull(latestLocation);
        Assert.True(latestLocation.IsOnline);
    }

    [Fact]
    public async Task AdvancedGpsSimulator_OfflineCellularDisruption_BuffersLocallyAndBatchFlushesOnReconnect()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedGpsSimulator_OfflineCellularDisruption_BuffersLocallyAndBatchFlushesOnReconnect));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var validationService = new GpsValidationService(_dateTimeProvider);
        var gpsIngestion = new GpsIngestionService(context, validationService, geofenceEvaluator, _dateTimeProvider, NullLogger<GpsIngestionService>.Instance);
        var simulator = new AdvancedGpsSimulator(context, gpsIngestion, _dateTimeProvider, NullLogger<AdvancedGpsSimulator>.Instance);

        var options = new AdvancedGpsSimulationOptions(
            TripId: env.TripId,
            CruiseSpeedKmh: 40m,
            StepSeconds: 4,
            StopDwellSeconds: 12,
            SimulateOfflineDisruption: true,
            DisruptionStartStep: 2,
            DisruptionDurationSteps: 4
        );

        // Act
        var result = await simulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OfflineBatchedPackets > 0);
        Assert.Contains(result.LogEvents, l => l.Contains("[OFFLINE_BUFFER]"));
        Assert.Contains(result.LogEvents, l => l.Contains("[CELLULAR_RECONNECT]") || l.Contains("[OFFLINE_FLUSH]"));

        // Verify all points persisted to location log table
        var logs = await context.LocationLogs.Where(l => l.BusId == env.BusId).ToListAsync();
        Assert.NotEmpty(logs);
    }

    [Fact]
    public async Task AdvancedGpsSimulator_DegradedSatelliteAccuracy_SafelyIngestsAndLogsHdopAlerts()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedGpsSimulator_DegradedSatelliteAccuracy_SafelyIngestsAndLogsHdopAlerts));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var validationService = new GpsValidationService(_dateTimeProvider);
        var gpsIngestion = new GpsIngestionService(context, validationService, geofenceEvaluator, _dateTimeProvider, NullLogger<GpsIngestionService>.Instance);
        var simulator = new AdvancedGpsSimulator(context, gpsIngestion, _dateTimeProvider, NullLogger<AdvancedGpsSimulator>.Instance);

        var options = new AdvancedGpsSimulationOptions(
            TripId: env.TripId,
            CruiseSpeedKmh: 30m,
            StepSeconds: 5,
            StopDwellSeconds: 10,
            SimulateDegradedGps: true,
            DegradedStartStep: 3,
            DegradedDurationSteps: 3
        );

        // Act
        var result = await simulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.LogEvents, l => l.Contains("[GPS_DEGRADED]"));
    }

    [Fact]
    public async Task AdvancedRfidSimulator_StopBoardingSweep_RegistersBoardingEvents()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedRfidSimulator_StopBoardingSweep_RegistersBoardingEvents));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var rfidIngestion = new RfidIngestionService(context, geofenceEvaluator, _dateTimeProvider, NullLogger<RfidIngestionService>.Instance);
        var rfidSimulator = new AdvancedRfidSimulator(context, rfidIngestion, _dateTimeProvider, NullLogger<AdvancedRfidSimulator>.Instance);

        var options = new AdvancedRfidSimulationOptions(
            TripId: env.TripId,
            Mode: "Boarding",
            IncludeRapidDoubleTap: false,
            IncludeUnknownCardTap: false
        );

        // Act
        var result = await rfidSimulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.BoardingEventsCount);
        Assert.Single(result.ScanResults);

        var scan = result.ScanResults[0];
        Assert.True(scan.Success);
        Assert.Equal("Boarding", scan.ScanType);
        Assert.Equal("Leo Martinez", scan.StudentName);

        // Verify boarding record persisted in database
        var boardingEvent = await context.BoardingEvents.FirstOrDefaultAsync(e => e.StudentId == env.StudentId);
        Assert.NotNull(boardingEvent);
        Assert.Equal("Boarding", boardingEvent.ScanType);
    }

    [Fact]
    public async Task AdvancedRfidSimulator_RapidDoubleTap_SafelyDeduplicated()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedRfidSimulator_RapidDoubleTap_SafelyDeduplicated));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var rfidIngestion = new RfidIngestionService(context, geofenceEvaluator, _dateTimeProvider, NullLogger<RfidIngestionService>.Instance);
        var rfidSimulator = new AdvancedRfidSimulator(context, rfidIngestion, _dateTimeProvider, NullLogger<AdvancedRfidSimulator>.Instance);

        var options = new AdvancedRfidSimulationOptions(
            TripId: env.TripId,
            Mode: "Boarding",
            IncludeRapidDoubleTap: true,
            IncludeUnknownCardTap: false
        );

        // Act
        var result = await rfidSimulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.BoardingEventsCount);
        Assert.Equal(1, result.DeduplicatedCount);
        Assert.Contains(result.EventTimeline, t => t.Contains("[DEDUP_VERIFIED]"));

        // Verify only 1 physical boarding event was recorded for this student
        var boardingCount = await context.BoardingEvents.CountAsync(e => e.StudentId == env.StudentId);
        Assert.Equal(1, boardingCount);
    }

    [Fact]
    public async Task AdvancedRfidSimulator_UnknownCardScan_LogsSecurityAuditWithoutThrowing()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedRfidSimulator_UnknownCardScan_LogsSecurityAuditWithoutThrowing));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var rfidIngestion = new RfidIngestionService(context, geofenceEvaluator, _dateTimeProvider, NullLogger<RfidIngestionService>.Instance);
        var rfidSimulator = new AdvancedRfidSimulator(context, rfidIngestion, _dateTimeProvider, NullLogger<AdvancedRfidSimulator>.Instance);

        var options = new AdvancedRfidSimulationOptions(
            TripId: env.TripId,
            Mode: "Boarding",
            IncludeRapidDoubleTap: false,
            IncludeUnknownCardTap: true
        );

        // Act
        var result = await rfidSimulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UnknownCardsFlagged);
        Assert.Contains(result.EventTimeline, t => t.Contains("[SECURITY_AUDIT]"));

        // Verify unknown card tap audit record
        var audit = await context.UnknownCardTaps.FirstOrDefaultAsync(u => u.CardUid == "XX-99-RO-GU-E1");
        Assert.NotNull(audit);
        Assert.Equal(env.RfidCode, audit.DeviceCode);
    }

    [Fact]
    public async Task AdvancedRfidSimulator_OfflineReaderQueue_BatchSynchronizedAccurately()
    {
        // Arrange
        using var context = CreateDbContext(nameof(AdvancedRfidSimulator_OfflineReaderQueue_BatchSynchronizedAccurately));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var rfidIngestion = new RfidIngestionService(context, geofenceEvaluator, _dateTimeProvider, NullLogger<RfidIngestionService>.Instance);
        var rfidSimulator = new AdvancedRfidSimulator(context, rfidIngestion, _dateTimeProvider, NullLogger<AdvancedRfidSimulator>.Instance);

        var options = new AdvancedRfidSimulationOptions(
            TripId: env.TripId,
            Mode: "Boarding",
            IncludeRapidDoubleTap: false,
            IncludeUnknownCardTap: false,
            SimulateOfflineReader: true
        );

        // Act
        var result = await rfidSimulator.RunSimulationAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.BoardingEventsCount);
        Assert.Contains(result.EventTimeline, t => t.Contains("[BATCH_SYNC]"));

        var boarding = await context.BoardingEvents.FirstOrDefaultAsync(e => e.StudentId == env.StudentId);
        Assert.NotNull(boarding);
        Assert.Equal("Boarding", boarding.ScanType);
    }

    [Fact]
    public async Task EndToEnd_GoldenPath_FullLifecycle_FromTripStartToDeboardingAndCompletion()
    {
        // Arrange
        using var context = CreateDbContext(nameof(EndToEnd_GoldenPath_FullLifecycle_FromTripStartToDeboardingAndCompletion));
        var env = SeedFullHardwareEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var validationService = new GpsValidationService(_dateTimeProvider);

        var gpsIngestion = new GpsIngestionService(context, validationService, geofenceEvaluator, _dateTimeProvider, NullLogger<GpsIngestionService>.Instance);
        var gpsSimulator = new AdvancedGpsSimulator(context, gpsIngestion, _dateTimeProvider, NullLogger<AdvancedGpsSimulator>.Instance);

        var rfidIngestion = new RfidIngestionService(context, geofenceEvaluator, _dateTimeProvider, NullLogger<RfidIngestionService>.Instance);
        var rfidSimulator = new AdvancedRfidSimulator(context, rfidIngestion, _dateTimeProvider, NullLogger<AdvancedRfidSimulator>.Instance);

        // 1. GPS Traversal: Bus moves through route
        var gpsOptions = new AdvancedGpsSimulationOptions(
            TripId: env.TripId,
            CruiseSpeedKmh: 45m,
            StepSeconds: 5,
            StopDwellSeconds: 15
        );

        var gpsResult = await gpsSimulator.RunSimulationAsync(gpsOptions);
        Assert.True(gpsResult.TotalPacketsIngested > 0);

        // 2. Boarding at Stop 2
        var rfidBoardingOptions = new AdvancedRfidSimulationOptions(
            TripId: env.TripId,
            Mode: "Boarding",
            IncludeRapidDoubleTap: true,
            IncludeUnknownCardTap: false
        );

        var rfidBoardingResult = await rfidSimulator.RunSimulationAsync(rfidBoardingOptions);
        Assert.Equal(1, rfidBoardingResult.BoardingEventsCount);

        // 3. Deboarding at Stop 3 (School Destination)
        var dropOffPacket = new RfidScanPacket(
            env.RfidCode,
            env.RfidKey,
            env.CardUid,
            37.7795m,
            -122.4140m,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "{\"lifecycle\":\"school-dropoff\"}"
        );

        var dropOffResult = await rfidIngestion.IngestTapAsync(dropOffPacket);
        Assert.True(dropOffResult.Success);
        Assert.Equal("DropOff", dropOffResult.ScanType);

        // 4. Trip Completion
        var trip = await context.Trips.FirstAsync(t => t.Id == env.TripId);
        trip.Status = TripStatuses.Completed;
        trip.ActualEndAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        // Assert Final State
        Assert.Equal(TripStatuses.Completed, trip.Status);
        var finalEvents = await context.BoardingEvents
            .Where(e => e.StudentId == env.StudentId)
            .OrderBy(e => e.ScannedAt)
            .ToListAsync();

        Assert.Equal(2, finalEvents.Count);
        Assert.Equal("Boarding", finalEvents[0].ScanType);
        Assert.Equal("DropOff", finalEvents[1].ScanType);
    }
}
