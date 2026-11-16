using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Features.Boarding;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Simulator;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies hardware-agnostic RFID card tap resolution, boarding/drop-off state transitions,
/// rapid double-tap deduplication, unknown card audit safety, batch sync, and trip passenger manifests.
/// </summary>
public sealed class RfidGatewayTests
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

    private static (Guid schoolId, Guid busId, Guid tripId, Guid studentId, string cardUid, string deviceCode, string apiKey)
        SeedFullTripAndStudentEnvironment(
            SchoolTrackDbContext context,
            string deviceCode = "RFID-UNIT-01",
            string apiKey = "RfidSecret123",
            string cardUid = "CARD-STU-001")
    {
        var schoolId = Guid.NewGuid();

        // 1. Bus & RFID reader
        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-RFID-01",
            RegistrationNumber = $"MH12RF{Guid.NewGuid():N}"[..12],
            Capacity = 30,
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        var rfidDevice = new RfidDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = deviceCode,
            ApiKey = apiKey,
            ProtocolType = "Simulator",
            Status = "Provisioning"
        };
        context.RfidDevices.Add(rfidDevice);

        // 2. Driver & User
        var driverUser = new User
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Email = $"driver.{Guid.NewGuid():N}@test.local",
            FullName = "Suresh Patil",
            Role = SystemRoles.Driver,
            PasswordHash = "HASH",
            IsActive = true
        };
        context.Users.Add(driverUser);

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = driverUser.Id,
            LicenseNumber = "DL-TEST-999",
            AvailabilityStatus = "OnTrip"
        };
        context.Drivers.Add(driver);

        // 3. Class & Student
        var classEntity = new Class
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Grade = "Grade 5",
            Division = "B",
            AcademicYear = "2026-2027"
        };
        context.Classes.Add(classEntity);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = classEntity.Id,
            AdmissionNumber = $"ADM-{Guid.NewGuid():N}"[..10],
            FirstName = "Aarav",
            LastName = "Sharma",
            IsActive = true
        };
        context.Students.Add(student);

        // 4. RFID Card assigned to student
        var rfidCard = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = student.Id,
            CardUid = cardUid,
            Status = "Active",
            IssuedAt = DateTimeOffset.UtcNow
        };
        context.RfidCards.Add(rfidCard);

        // 5. Route with 2 stops
        var route = new Route
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Code = "R-RFID",
            Name = "RFID Test Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);

        var stop1 = new RouteStop
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Pickup Stop",
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
            StopName = "School Campus",
            SequenceOrder = 2,
            Latitude = 18.540000m,
            Longitude = 73.870000m,
            GeofenceRadiusMeters = 50
        };
        context.RouteStops.AddRange(stop1, stop2);

        // 6. Active Trip
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            BusId = bus.Id,
            DriverId = driver.Id,
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.Started,
            ActualStartAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        context.Trips.Add(trip);

        var tripStop1 = new TripStop { Id = Guid.NewGuid(), SchoolId = schoolId, TripId = trip.Id, RouteStopId = stop1.Id, SequenceOrder = 1, Status = TripStopStatuses.Pending };
        var tripStop2 = new TripStop { Id = Guid.NewGuid(), SchoolId = schoolId, TripId = trip.Id, RouteStopId = stop2.Id, SequenceOrder = 2, Status = TripStopStatuses.Pending };
        context.TripStops.AddRange(tripStop1, tripStop2);

        context.SaveChanges();
        return (schoolId, bus.Id, trip.Id, student.Id, cardUid, deviceCode, apiKey);
    }

    [Fact]
    public async Task RfidTap_FirstScan_RecordsBoardingEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, studentId, cardUid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var packetTime = _dateTimeProvider.UtcNow;
        var packet = new RfidScanPacket(
            deviceCode,
            apiKey,
            cardUid,
            18.520410m,
            73.856710m,
            packetTime);

        var result = await ingestionService.IngestTapAsync(packet);

        Assert.True(result.Success);
        Assert.Equal(BoardingScanTypes.Boarding, result.ScanType);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(tripId, result.TripId);
        Assert.Equal("Pickup Stop", result.StopName);

        // Verify database persistence
        var boardingEvent = await context.BoardingEvents.FirstOrDefaultAsync(b => b.TripId == tripId && b.StudentId == studentId);
        Assert.NotNull(boardingEvent);
        Assert.Equal(BoardingScanTypes.Boarding, boardingEvent.ScanType);
        Assert.Equal(busId, boardingEvent.BusId);
        Assert.Equal(packetTime, boardingEvent.ScannedAt);

        // Verify RfidDevice status updated
        var device = await context.RfidDevices.FirstAsync(d => d.DeviceCode == deviceCode);
        Assert.Equal("Online", device.Status);
        Assert.NotNull(device.LastScanAt);
    }

    [Fact]
    public async Task RfidTap_SecondScan_RecordsDropOffEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, studentId, cardUid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        // First tap: Boarding at Stop 1
        var t1 = _dateTimeProvider.UtcNow.AddMinutes(-10);
        await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, cardUid, 18.520410m, 73.856710m, t1));

        // Second tap: Drop-off at Stop 2 (10 minutes later)
        var t2 = _dateTimeProvider.UtcNow;
        var dropOffResult = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, cardUid, 18.540010m, 73.870010m, t2));

        Assert.True(dropOffResult.Success);
        Assert.Equal(BoardingScanTypes.DropOff, dropOffResult.ScanType);
        Assert.Equal(studentId, dropOffResult.StudentId);
        Assert.Equal("School Campus", dropOffResult.StopName);

        // Verify two events in database
        var events = await context.BoardingEvents
            .Where(b => b.TripId == tripId && b.StudentId == studentId)
            .OrderBy(b => b.ScannedAt)
            .ToListAsync();

        Assert.Equal(2, events.Count);
        Assert.Equal(BoardingScanTypes.Boarding, events[0].ScanType);
        Assert.Equal(BoardingScanTypes.DropOff, events[1].ScanType);
    }

    [Fact]
    public async Task RfidTap_RapidDoubleTap_DeduplicatedWithinCooldown()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, studentId, cardUid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var t1 = _dateTimeProvider.UtcNow;
        var firstResult = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, cardUid, 18.520410m, 73.856710m, t1));
        Assert.True(firstResult.Success);

        // Immediate double-tap 2 seconds later
        var t2 = t1.AddSeconds(2);
        var secondResult = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, cardUid, 18.520410m, 73.856710m, t2));

        Assert.False(secondResult.Success);
        Assert.Contains("15-second cooldown", secondResult.Message);

        // Exactly 1 boarding event exists
        var eventCount = await context.BoardingEvents.CountAsync(b => b.TripId == tripId && b.StudentId == studentId);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task RfidTap_UnregisteredCard_LoggedToUnknownCardTapsWithoutException()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, _, _, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var unknownUid = "CARD-UNKNOWN-999";
        var result = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, unknownUid, 18.52m, 73.85m, _dateTimeProvider.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("not registered", result.Message);

        // Verify UnknownCardTap audit table
        var unknownTap = await context.UnknownCardTaps.FirstOrDefaultAsync(u => u.CardUid == unknownUid);
        Assert.NotNull(unknownTap);
        Assert.Equal("UnregisteredCard", unknownTap.Reason);
        Assert.Equal(deviceCode, unknownTap.DeviceCode);
        Assert.Equal(busId, unknownTap.BusId);
        Assert.Equal(tripId, unknownTap.TripId);
    }

    [Fact]
    public async Task RfidTap_UnassignedCard_LoggedToUnknownCardTaps()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, _, _, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        // Create registered card with NO student assigned
        var unassignedCard = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = null,
            CardUid = "CARD-SPARE-001",
            Status = "Unassigned"
        };
        context.RfidCards.Add(unassignedCard);
        await context.SaveChangesAsync();

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var result = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, "CARD-SPARE-001", 18.52m, 73.85m, _dateTimeProvider.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("not assigned to any student", result.Message);

        var unknownTap = await context.UnknownCardTaps.FirstOrDefaultAsync(u => u.CardUid == "CARD-SPARE-001");
        Assert.NotNull(unknownTap);
        Assert.Equal("UnassignedCard", unknownTap.Reason);
    }

    [Fact]
    public async Task RfidTap_NoActiveTripOnBus_LoggedSafely()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, _, cardUid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        // Complete the active trip so no active trip is running on the bus
        var trip = await context.Trips.FirstAsync(t => t.Id == tripId);
        trip.Status = TripStatuses.Completed;
        await context.SaveChangesAsync();

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var result = await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, cardUid, 18.52m, 73.85m, _dateTimeProvider.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("No active trip", result.Message);

        var unknownTap = await context.UnknownCardTaps.FirstOrDefaultAsync(u => u.Reason == "NoActiveTrip");
        Assert.NotNull(unknownTap);
    }

    [Fact]
    public async Task RfidBatchIngestion_PreservesChronology_MultipleStudentsHandled()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, student1Id, card1Uid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        // Add second student and card
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = (await context.Classes.FirstAsync()).Id,
            AdmissionNumber = "ADM-STU-002",
            FirstName = "Diya",
            LastName = "Patel"
        };
        context.Students.Add(student2);

        var card2 = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = student2.Id,
            CardUid = "CARD-STU-002",
            Status = "Active"
        };
        context.RfidCards.Add(card2);
        await context.SaveChangesAsync();

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var t0 = _dateTimeProvider.UtcNow.AddMinutes(-5);
        var t1 = _dateTimeProvider.UtcNow.AddMinutes(-4);

        var batchRequest = new RfidBatchScanRequest(
            deviceCode,
            apiKey,
            new List<RfidScanPointDto>
            {
                new(card1Uid, 18.52m, 73.85m, t0),
                new("CARD-STU-002", 18.52m, 73.85m, t1)
            });

        var results = await ingestionService.IngestBatchTapsAsync(batchRequest);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Equal(BoardingScanTypes.Boarding, r.ScanType));

        var events = await context.BoardingEvents.Where(b => b.TripId == tripId).ToListAsync();
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task RfidSimulator_SimulatesStudentTap_GeneratesValidBoardingEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, studentId, _, _, _) =
            SeedFullTripAndStudentEnvironment(context);

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        var simulator = new RfidSimulatorService(context, ingestionService, _dateTimeProvider);

        var stop = await context.RouteStops.FirstAsync();
        var simResult = await simulator.SimulateTapAsync(new SimulateRfidTapRequest(tripId, studentId, stop.Id));

        Assert.True(simResult.Success);
        Assert.Equal(BoardingScanTypes.Boarding, simResult.ScanType);
        Assert.Equal(studentId, simResult.StudentId);
        Assert.Equal("Pickup Stop", simResult.StopName);
    }

    [Fact]
    public async Task TripPassengerManifestQuery_ReflectsAccurateStudentBoardingState()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName);
        var (schoolId, busId, tripId, student1Id, card1Uid, deviceCode, apiKey) =
            SeedFullTripAndStudentEnvironment(context);

        // Add second student
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = (await context.Classes.FirstAsync()).Id,
            AdmissionNumber = "ADM-MANIFEST-02",
            FirstName = "Kavya",
            LastName = "Iyer"
        };
        context.Students.Add(student2);

        var card2 = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = student2.Id,
            CardUid = "CARD-KAVYA-02",
            Status = "Active"
        };
        context.RfidCards.Add(card2);
        await context.SaveChangesAsync();

        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance);

        // Student 1: Boards and stays on bus
        await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, card1Uid, 18.520410m, 73.856710m, _dateTimeProvider.UtcNow.AddMinutes(-10)));

        // Student 2: Boards at Stop 1 and Drops off at Stop 2
        await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, "CARD-KAVYA-02", 18.520410m, 73.856710m, _dateTimeProvider.UtcNow.AddMinutes(-8)));

        await ingestionService.IngestTapAsync(new RfidScanPacket(
            deviceCode, apiKey, "CARD-KAVYA-02", 18.540010m, 73.870010m, _dateTimeProvider.UtcNow));

        // Query Manifest
        var manifestHandler = new GetTripPassengerManifestQueryHandler(context);
        var manifest = await manifestHandler.HandleAsync(new GetTripPassengerManifestQuery(tripId));

        Assert.Equal(tripId, manifest.TripId);
        Assert.Equal(2, manifest.TotalStudents);
        Assert.Equal(1, manifest.BoardedCount); // Student 1
        Assert.Equal(1, manifest.DroppedOffCount); // Student 2

        var p1 = manifest.Passengers.First(p => p.StudentId == student1Id);
        Assert.Equal("Boarded", p1.CurrentStatus);
        Assert.Equal("Pickup Stop", p1.BoardedStopName);
        Assert.Null(p1.DroppedOffAt);

        var p2 = manifest.Passengers.First(p => p.StudentId == student2.Id);
        Assert.Equal("DroppedOff", p2.CurrentStatus);
        Assert.Equal("Pickup Stop", p2.BoardedStopName);
        Assert.Equal("School Campus", p2.DroppedOffStopName);
    }
}
