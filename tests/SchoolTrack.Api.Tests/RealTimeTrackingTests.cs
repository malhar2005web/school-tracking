using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Api.Hubs;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Features.Trips;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Validation;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies SignalR real-time dispatching for live GPS updates, RFID student boarding alerts,
/// trip status transitions, and the background heartbeat connectivity watchdog.
/// </summary>
public sealed class RealTimeTrackingTests
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

    [Fact]
    public void TrackingHub_GroupNamingConventions_ConsistentAndIsolated()
    {
        var id = Guid.NewGuid();
        Assert.Equal($"school_{id:N}", TrackingHub.SchoolGroup(id));
        Assert.Equal($"trip_{id:N}", TrackingHub.TripGroup(id));
        Assert.Equal($"bus_{id:N}", TrackingHub.BusGroup(id));
        Assert.Equal($"parent_{id:N}", TrackingHub.ParentGroup(id));
    }

    [Fact]
    public async Task GpsIngestion_BroadcastsLiveBusLocation_ToTrackingChannels()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        await using var context = CreateDbContext(dbName, schoolId);

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-RT-01",
            RegistrationNumber = "MH12RT1001",
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        var gpsDevice = new GpsDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = "GPS-RT-01",
            ApiKey = "KeyRT",
            Status = "Provisioning"
        };
        context.GpsDevices.Add(gpsDevice);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = Guid.NewGuid(),
            BusId = bus.Id,
            DriverId = Guid.NewGuid(),
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.InTransit
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var broadcaster = new TestTrackingBroadcaster();
        var validationService = new GpsValidationService(_dateTimeProvider);
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new GpsIngestionService(
            context,
            validationService,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<GpsIngestionService>.Instance,
            broadcaster);

        var packet = new GpsTelemetryPacket(
            "GPS-RT-01",
            "KeyRT",
            null,
            18.5204m,
            73.8567m,
            38.5m,
            120m,
            null,
            null,
            true,
            _dateTimeProvider.UtcNow,
            null);

        var result = await ingestionService.IngestAsync(packet);
        Assert.True(result.Success);

        // Verify broadcaster received live location event
        Assert.Single(broadcaster.BroadcastedLocations);
        var broadcasted = broadcaster.BroadcastedLocations[0];
        Assert.Equal(bus.Id, broadcasted.BusId);
        Assert.Equal(trip.Id, broadcasted.TripId);
        Assert.Equal(18.5204m, broadcasted.Latitude);
        Assert.Equal(73.8567m, broadcasted.Longitude);
        Assert.Equal(38.5m, broadcasted.SpeedKmh);
        Assert.True(broadcasted.IsOnline);
    }

    [Fact]
    public async Task RfidTap_BroadcastsLiveBoardingNotification_ToLinkedParentsPrivateChannel()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        await using var context = CreateDbContext(dbName, schoolId);

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-BOARD-01",
            RegistrationNumber = "MH12BOARD01",
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        var rfidDevice = new RfidDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = "RFID-BOARD-01",
            ApiKey = "KeyBoard",
            Status = "Provisioning"
        };
        context.RfidDevices.Add(rfidDevice);

        var classEntity = new Class { Id = Guid.NewGuid(), SchoolId = schoolId, Grade = "Grade 4", Division = "A" };
        context.Classes.Add(classEntity);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = classEntity.Id,
            AdmissionNumber = "ADM-RT-01",
            FirstName = "Ananya",
            LastName = "Deshmukh",
            IsActive = true
        };
        context.Students.Add(student);

        // Link Parent to Student
        var parentUserId = Guid.NewGuid();
        var parentUser = new User
        {
            Id = parentUserId,
            SchoolId = schoolId,
            Email = "parent.ananya@schooltrack.local",
            FullName = "Sunil Deshmukh",
            Role = SystemRoles.Parent,
            PasswordHash = "HASH"
        };
        context.Users.Add(parentUser);

        var parent = new Parent
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = parentUser.Id,
            PrimaryPhone = "+919811122233"
        };
        context.Parents.Add(parent);

        var parentLink = new ParentStudent
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ParentId = parent.Id,
            StudentId = student.Id,
            Relationship = "Father",
            IsPrimaryGuardian = true
        };
        context.ParentStudents.Add(parentLink);

        var rfidCard = new RfidCard
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentId = student.Id,
            CardUid = "CARD-ANANYA-01",
            Status = "Active"
        };
        context.RfidCards.Add(rfidCard);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = Guid.NewGuid(),
            BusId = bus.Id,
            DriverId = Guid.NewGuid(),
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.Started
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var broadcaster = new TestTrackingBroadcaster();
        var geofenceEvaluator = new GeofenceEvaluator();
        var ingestionService = new RfidIngestionService(
            context,
            geofenceEvaluator,
            _dateTimeProvider,
            NullLogger<RfidIngestionService>.Instance,
            broadcaster);

        var packet = new RfidScanPacket(
            "RFID-BOARD-01",
            "KeyBoard",
            "CARD-ANANYA-01",
            18.52m,
            73.85m,
            _dateTimeProvider.UtcNow);

        var result = await ingestionService.IngestTapAsync(packet);
        Assert.True(result.Success);

        // Verify broadcaster dispatched boarding alert
        Assert.Single(broadcaster.BroadcastedBoardingAlerts);
        var (notification, parents) = broadcaster.BroadcastedBoardingAlerts[0];

        Assert.Equal(student.Id, notification.StudentId);
        Assert.Equal("Ananya Deshmukh", notification.StudentName);
        Assert.Equal(BoardingScanTypes.Boarding, notification.ScanType);
        Assert.Contains("boarded", notification.Message);

        // Verified parent private channel target
        Assert.Single(parents);
        Assert.Equal(parentUserId, parents[0]);
    }

    [Fact]
    public async Task TripStatusTransition_BroadcastsStatusChangedEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        await using var context = CreateDbContext(dbName, schoolId);

        var bus = new Bus { Id = Guid.NewGuid(), SchoolId = schoolId, BusNumber = "BUS-STATUS-01", RegistrationNumber = "MH12ST01" };
        context.Buses.Add(bus);

        var driverUser = new User { Id = Guid.NewGuid(), SchoolId = schoolId, Email = "driver@test.local", FullName = "Driver One", Role = SystemRoles.Driver, PasswordHash = "H" };
        context.Users.Add(driverUser);

        var driver = new Driver { Id = Guid.NewGuid(), SchoolId = schoolId, UserId = driverUser.Id };
        context.Drivers.Add(driver);

        var route = new Route { Id = Guid.NewGuid(), SchoolId = schoolId, Code = "R-STAT", Name = "Status Route" };
        context.Routes.Add(route);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = route.Id,
            BusId = bus.Id,
            DriverId = driver.Id,
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Shift = TripShifts.Morning,
            Status = TripStatuses.Scheduled
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var broadcaster = new TestTrackingBroadcaster();
        var tenantContext = new TestTenant(schoolId, false);
        var driverCurrentUser = new TestCurrentUser(driverUser.Id, schoolId, SystemRoles.Driver);

        var handler = new UpdateTripStatusCommandHandler(context, tenantContext, driverCurrentUser, _dateTimeProvider, broadcaster);

        await handler.HandleAsync(new UpdateTripStatusCommand(trip.Id, TripStatuses.Started));

        // Verify broadcast event
        Assert.Single(broadcaster.BroadcastedTripStatusEvents);
        var statusEvent = broadcaster.BroadcastedTripStatusEvents[0];
        Assert.Equal(trip.Id, statusEvent.TripId);
        Assert.Equal(TripStatuses.Scheduled, statusEvent.PreviousStatus);
        Assert.Equal(TripStatuses.Started, statusEvent.NewStatus);
    }

    [Fact]
    public async Task TripStopStatusTransition_BroadcastsStopStatusChangedEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        await using var context = CreateDbContext(dbName, schoolId);

        var route = new Route { Id = Guid.NewGuid(), SchoolId = schoolId, Code = "R-STOP", Name = "Stop Route" };
        context.Routes.Add(route);

        var routeStop = new RouteStop { Id = Guid.NewGuid(), SchoolId = schoolId, RouteId = route.Id, StopName = "Main Gate", SequenceOrder = 1 };
        context.RouteStops.Add(routeStop);

        var trip = new Trip { Id = Guid.NewGuid(), SchoolId = schoolId, RouteId = route.Id, BusId = Guid.NewGuid(), DriverId = Guid.NewGuid(), Status = TripStatuses.Started };
        context.Trips.Add(trip);

        var tripStop = new TripStop { Id = Guid.NewGuid(), SchoolId = schoolId, TripId = trip.Id, RouteStopId = routeStop.Id, SequenceOrder = 1, Status = TripStopStatuses.Pending };
        context.TripStops.Add(tripStop);
        await context.SaveChangesAsync();

        var broadcaster = new TestTrackingBroadcaster();
        var tenantContext = new TestTenant(schoolId, false);

        var handler = new UpdateTripStopStatusCommandHandler(context, tenantContext, _dateTimeProvider, broadcaster);
        await handler.HandleAsync(new UpdateTripStopStatusCommand(trip.Id, tripStop.Id, TripStopStatuses.Arrived));

        // Verify broadcast event
        Assert.Single(broadcaster.BroadcastedStopStatusEvents);
        var stopEvent = broadcaster.BroadcastedStopStatusEvents[0];
        Assert.Equal(trip.Id, stopEvent.TripId);
        Assert.Equal(tripStop.Id, stopEvent.StopId);
        Assert.Equal("Main Gate", stopEvent.StopName);
        Assert.Equal(TripStopStatuses.Arrived, stopEvent.Status);
    }

    [Fact]
    public async Task BusHeartbeatWatchdog_IdentifiesStaleBuses_AndBroadcastsOfflineAlert()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusNumber = "BUS-WATCH-01",
            RegistrationNumber = "MH12WATCH01",
            MaintenanceStatus = "Active",
            IsActive = true
        };

        var gpsDevice = new GpsDevice
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            DeviceCode = "GPS-WATCH-01",
            Status = "Online"
        };

        // Telemetry recorded 4 minutes ago (> 90 second threshold)
        var staleLocation = new LatestBusLocation
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            BusId = bus.Id,
            Latitude = 18.52m,
            Longitude = 73.85m,
            SpeedKmh = 0m,
            Heading = 0m,
            IsOnline = true,
            RecordedAt = _dateTimeProvider.UtcNow.AddMinutes(-4)
        };

        var services = new ServiceCollection();
        var broadcaster = new TestTrackingBroadcaster();

        services.AddScoped<IApplicationDbContext>(_ =>
        {
            var ctx = CreateDbContext(dbName, schoolId);
            return ctx;
        });
        services.AddSingleton<ITrackingBroadcaster>(broadcaster);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Seed initial data
        await using (var seedCtx = CreateDbContext(dbName, schoolId))
        {
            seedCtx.Buses.Add(bus);
            seedCtx.GpsDevices.Add(gpsDevice);
            seedCtx.LatestBusLocations.Add(staleLocation);
            await seedCtx.SaveChangesAsync();
        }

        // Run watchdog cycle
        var watchdog = new BusHeartbeatMonitorHostedService(
            scopeFactory,
            _dateTimeProvider,
            NullLogger<BusHeartbeatMonitorHostedService>.Instance);

        await watchdog.CheckStaleBusesAsync();

        // Verify database state: IsOnline marked false, device Status = Offline
        await using (var verifyCtx = CreateDbContext(dbName, schoolId))
        {
            var loc = await verifyCtx.LatestBusLocations.FirstAsync(l => l.BusId == bus.Id);
            Assert.False(loc.IsOnline);

            var dev = await verifyCtx.GpsDevices.FirstAsync(d => d.Id == gpsDevice.Id);
            Assert.Equal("Offline", dev.Status);
        }

        // Verify broadcast event
        Assert.Single(broadcaster.BroadcastedConnectivityEvents);
        var alert = broadcaster.BroadcastedConnectivityEvents[0];
        Assert.Equal(bus.Id, alert.BusId);
        Assert.False(alert.IsOnline);
    }
}
