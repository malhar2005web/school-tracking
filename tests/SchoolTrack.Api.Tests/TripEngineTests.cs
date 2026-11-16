using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Features.Routes;
using SchoolTrack.Application.Features.Trips;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Authentication;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies Route planning, Trip generation, Trip state machine, stop transitions, driver scoping, and multi-tenant isolation.
/// </summary>
public sealed class TripEngineTests
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

    private static (Guid busId, Guid driverId, Guid driverUserId) SeedBusAndDriver(SchoolTrackDbContext context, Guid schoolId)
    {
        var bus = new Bus
        {
            SchoolId = schoolId,
            BusNumber = "BUS-TE-01",
            RegistrationNumber = "MH12TE1001",
            Capacity = 40,
            MaintenanceStatus = "Active",
            IsActive = true
        };
        context.Buses.Add(bus);

        var driverUser = new User
        {
            SchoolId = schoolId,
            Email = $"driver.{Guid.NewGuid():N}@schooltrack.local",
            PasswordHash = "HASH",
            FullName = "Ramesh Kumar",
            Role = SystemRoles.Driver,
            IsActive = true
        };
        context.Users.Add(driverUser);

        var driver = new Driver
        {
            SchoolId = schoolId,
            UserId = driverUser.Id,
            LicenseNumber = $"DL-TE-{Guid.NewGuid():N}"[..15],
            LicenseExpiry = DateTimeOffset.UtcNow.AddYears(2),
            Phone = "+919876543210",
            AvailabilityStatus = "Available"
        };
        context.Drivers.Add(driver);

        context.SaveChanges();
        return (bus.Id, driver.Id, driverUser.Id);
    }

    [Fact]
    public async Task RouteAndStops_Creation_PreservesSequenceOrder()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);

        var createRouteHandler = new CreateRouteCommandHandler(context, tenantContext);
        var addStopHandler = new AddRouteStopCommandHandler(context, tenantContext);
        var getRouteByIdHandler = new GetRouteByIdQueryHandler(context);

        // 1. Create Route
        var routeDto = await createRouteHandler.HandleAsync(new CreateRouteCommand("North Campus Express", "R-101", RouteDirectionTypes.MorningPickup));
        Assert.NotNull(routeDto);
        Assert.Equal("R-101", routeDto.Code);
        Assert.Equal("North Campus Express", routeDto.Name);

        // 2. Add Stops out of order
        await addStopHandler.HandleAsync(new AddRouteStopCommand(routeDto.Id, "Stop C - North Gate", 3, 18.5204m, 73.8567m, 30, 50));
        await addStopHandler.HandleAsync(new AddRouteStopCommand(routeDto.Id, "Stop A - Green Park", 1, 18.5100m, 73.8400m, 0, 50));
        await addStopHandler.HandleAsync(new AddRouteStopCommand(routeDto.Id, "Stop B - Oak Ridge", 2, 18.5150m, 73.8480m, 15, 50));

        // 3. Query Route
        var fetchedRoute = await getRouteByIdHandler.HandleAsync(new GetRouteByIdQuery(routeDto.Id));
        Assert.Equal(3, fetchedRoute.Stops.Count);
        Assert.Equal(1, fetchedRoute.Stops[0].SequenceOrder);
        Assert.Equal("Stop A - Green Park", fetchedRoute.Stops[0].StopName);
        Assert.Equal(2, fetchedRoute.Stops[1].SequenceOrder);
        Assert.Equal("Stop B - Oak Ridge", fetchedRoute.Stops[1].StopName);
        Assert.Equal(3, fetchedRoute.Stops[2].SequenceOrder);
        Assert.Equal("Stop C - North Gate", fetchedRoute.Stops[2].StopName);
    }

    [Fact]
    public async Task CreateTrip_InstantiatesTripStops_FromRouteStops()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);
        var currentUser = new TestCurrentUser(Guid.NewGuid(), schoolId, SystemRoles.SchoolAdmin);

        var (busId, driverId, _) = SeedBusAndDriver(context, schoolId);

        // Create route and stops
        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-201",
            Name = "South Campus Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        context.RouteStops.AddRange(
            new RouteStop { SchoolId = schoolId, RouteId = route.Id, StopName = "Stop 1", SequenceOrder = 1, Latitude = 18.50m, Longitude = 73.80m },
            new RouteStop { SchoolId = schoolId, RouteId = route.Id, StopName = "Stop 2", SequenceOrder = 2, Latitude = 18.51m, Longitude = 73.81m },
            new RouteStop { SchoolId = schoolId, RouteId = route.Id, StopName = "Stop 3", SequenceOrder = 3, Latitude = 18.52m, Longitude = 73.82m }
        );
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, currentUser, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driverId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Morning));

        Assert.NotNull(tripDto);
        Assert.Equal(TripStatuses.Scheduled, tripDto.Status);
        Assert.Equal(3, tripDto.Stops.Count);
        Assert.All(tripDto.Stops, s => Assert.Equal(TripStopStatuses.Pending, s.Status));
        Assert.Equal("Stop 1", tripDto.Stops[0].StopName);
        Assert.Equal(1, tripDto.Stops[0].SequenceOrder);
        Assert.Equal("Stop 3", tripDto.Stops[2].StopName);
        Assert.Equal(3, tripDto.Stops[2].SequenceOrder);
    }

    [Fact]
    public async Task TripStateMachine_ValidTransitions_SucceedAndRecordTimestamps()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);
        var (busId, driverId, driverUserId) = SeedBusAndDriver(context, schoolId);
        var driverUser = new TestCurrentUser(driverUserId, schoolId, SystemRoles.Driver);

        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-301",
            Name = "East Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, driverUser, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driverId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Morning));

        var updateStatusHandler = new UpdateTripStatusCommandHandler(context, tenantContext, driverUser, _dateTimeProvider);

        // Transition: Scheduled -> Started
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Started));
        var trip = await context.Trips.Include(t => t.StatusHistory).FirstAsync(t => t.Id == tripDto.Id);
        Assert.Equal(TripStatuses.Started, trip.Status);
        Assert.NotNull(trip.ActualStartAt);
        Assert.Null(trip.ActualEndAt);
        // Includes initial scheduled creation audit record + transition to Started
        Assert.Equal(2, trip.StatusHistory.Count);
        var latestHistory = trip.StatusHistory.OrderByDescending(h => h.ChangedAt).First();
        Assert.Equal(TripStatuses.Scheduled, latestHistory.FromStatus);
        Assert.Equal(TripStatuses.Started, latestHistory.ToStatus);

        // Transition: Started -> InTransit
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.InTransit));
        trip = await context.Trips.Include(t => t.StatusHistory).FirstAsync(t => t.Id == tripDto.Id);
        Assert.Equal(TripStatuses.InTransit, trip.Status);
        Assert.Equal(3, trip.StatusHistory.Count);

        // Transition: InTransit -> Arrived
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Arrived));
        trip = await context.Trips.Include(t => t.StatusHistory).FirstAsync(t => t.Id == tripDto.Id);
        Assert.Equal(TripStatuses.Arrived, trip.Status);
        Assert.Equal(4, trip.StatusHistory.Count);

        // Transition: Arrived -> Completed
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Completed));
        trip = await context.Trips.Include(t => t.StatusHistory).FirstAsync(t => t.Id == tripDto.Id);
        Assert.Equal(TripStatuses.Completed, trip.Status);
        Assert.NotNull(trip.ActualEndAt);
        Assert.Equal(5, trip.StatusHistory.Count);
    }

    [Fact]
    public async Task TripStateMachine_InvalidTransitions_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);
        var (busId, driverId, _) = SeedBusAndDriver(context, schoolId);
        var adminUser = new TestCurrentUser(Guid.NewGuid(), schoolId, SystemRoles.SchoolAdmin);

        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-401",
            Name = "Express",
            DirectionType = RouteDirectionTypes.AfternoonDrop,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, adminUser, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driverId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Afternoon));

        var updateStatusHandler = new UpdateTripStatusCommandHandler(context, tenantContext, adminUser, _dateTimeProvider);

        // Cannot jump directly from Scheduled to Completed
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Completed)));

        // Cannot jump directly from Scheduled to InTransit
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.InTransit)));
    }

    [Fact]
    public async Task TripCancellation_FromAnyActiveState_RecordsCancellationReason()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);
        var (busId, driverId, _) = SeedBusAndDriver(context, schoolId);
        var adminUser = new TestCurrentUser(Guid.NewGuid(), schoolId, SystemRoles.SchoolAdmin);

        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-501",
            Name = "Cancellation Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, adminUser, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driverId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Morning));

        var updateStatusHandler = new UpdateTripStatusCommandHandler(context, tenantContext, adminUser, _dateTimeProvider);

        // Start trip first
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Started));

        // Now cancel with reason
        await updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Cancelled, "Engine failure - bus replaced"));

        var trip = await context.Trips.FirstAsync(t => t.Id == tripDto.Id);
        Assert.Equal(TripStatuses.Cancelled, trip.Status);
        Assert.Equal("Engine failure - bus replaced", trip.CancellationReason);

        // Once cancelled, cannot transition to any other status
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.InTransit)));
    }

    [Fact]
    public async Task TripStopStatus_Update_RecordsArrivalAndDepartureTimestamps()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);
        var (busId, driverId, driverUserId) = SeedBusAndDriver(context, schoolId);
        var driverUser = new TestCurrentUser(driverUserId, schoolId, SystemRoles.Driver);

        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-601",
            Name = "Stop Route",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var routeStop = new RouteStop
        {
            SchoolId = schoolId,
            RouteId = route.Id,
            StopName = "Main Gate",
            SequenceOrder = 1,
            Latitude = 18.55m,
            Longitude = 73.88m
        };
        context.RouteStops.Add(routeStop);
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, driverUser, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driverId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Morning));

        var tripStopId = tripDto.Stops.First().Id;
        var updateStopHandler = new UpdateTripStopStatusCommandHandler(context, tenantContext, _dateTimeProvider);

        // Arrive at stop
        await updateStopHandler.HandleAsync(new UpdateTripStopStatusCommand(tripDto.Id, tripStopId, TripStopStatuses.Arrived));
        var stop = await context.TripStops.FirstAsync(s => s.Id == tripStopId);
        Assert.Equal(TripStopStatuses.Arrived, stop.Status);
        Assert.NotNull(stop.ActualArrivalAt);
        Assert.Null(stop.ActualDepartureAt);

        // Depart stop
        await updateStopHandler.HandleAsync(new UpdateTripStopStatusCommand(tripDto.Id, tripStopId, TripStopStatuses.Departed));
        stop = await context.TripStops.FirstAsync(s => s.Id == tripStopId);
        Assert.Equal(TripStopStatuses.Departed, stop.Status);
        Assert.NotNull(stop.ActualArrivalAt);
        Assert.NotNull(stop.ActualDepartureAt);
    }

    [Fact]
    public async Task DriverScoping_DriverCannotTransitionAnotherDriversTrip_ThrowsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new TestTenant(schoolId, false);

        var (busId, driver1Id, driver1UserId) = SeedBusAndDriver(context, schoolId);
        var (_, _, driver2UserId) = SeedBusAndDriver(context, schoolId);

        var driver1User = new TestCurrentUser(driver1UserId, schoolId, SystemRoles.Driver);
        var driver2User = new TestCurrentUser(driver2UserId, schoolId, SystemRoles.Driver);

        var route = new Route
        {
            SchoolId = schoolId,
            Code = "R-701",
            Name = "Route 701",
            DirectionType = RouteDirectionTypes.MorningPickup,
            IsActive = true
        };
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var createTripHandler = new CreateTripCommandHandler(context, tenantContext, driver1User, _dateTimeProvider);
        var tripDto = await createTripHandler.HandleAsync(new CreateTripCommand(
            route.Id,
            busId,
            driver1Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            TripShifts.Morning));

        // Driver 2 tries to update Driver 1's trip
        var updateStatusHandler = new UpdateTripStatusCommandHandler(context, tenantContext, driver2User, _dateTimeProvider);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            updateStatusHandler.HandleAsync(new UpdateTripStatusCommand(tripDto.Id, TripStatuses.Started)));
    }

    [Fact]
    public async Task TenantIsolation_SchoolAdmin_CannotSeeOtherSchoolRoutesOrTrips()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();

        // 1. Seed School A route and trip
        await using (var contextA = CreateDbContext(dbName, schoolAId))
        {
            var tenantA = new TestTenant(schoolAId, false);
            var userA = new TestCurrentUser(Guid.NewGuid(), schoolAId, SystemRoles.SchoolAdmin);
            var (busAId, driverAId, _) = SeedBusAndDriver(contextA, schoolAId);

            var routeA = new Route
            {
                SchoolId = schoolAId,
                Code = "R-SCHA",
                Name = "School A Express",
                DirectionType = RouteDirectionTypes.MorningPickup,
                IsActive = true
            };
            contextA.Routes.Add(routeA);
            await contextA.SaveChangesAsync();

            var createTripHandler = new CreateTripCommandHandler(contextA, tenantA, userA, _dateTimeProvider);
            await createTripHandler.HandleAsync(new CreateTripCommand(
                routeA.Id,
                busAId,
                driverAId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                TripShifts.Morning));
        }

        // 2. Query from School B context
        await using (var contextB = CreateDbContext(dbName, schoolBId))
        {
            var getRoutesHandler = new GetRoutesQueryHandler(contextB);
            var getTripsHandler = new GetTripsQueryHandler(contextB);

            var routesB = await getRoutesHandler.HandleAsync(new GetRoutesQuery());
            var tripsB = await getTripsHandler.HandleAsync(new GetTripsQuery());

            Assert.Empty(routesB);
            Assert.Empty(tripsB);
        }
    }
}
