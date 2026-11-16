using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.DriverApp.Models;
using SchoolTrack.DriverApp.Services;
using Xunit;

namespace SchoolTrack.Api.Tests;

public class DriverApplicationTests
{
    [Fact]
    public void DriverAuthState_DefaultDemoSession_InitializedWithRobertJenkinsAndBus14()
    {
        // Arrange & Act
        var authState = new DriverAuthState();

        // Assert
        Assert.True(authState.IsAuthenticated);
        Assert.NotNull(authState.CurrentSession);
        Assert.Equal("Robert Jenkins", authState.CurrentSession.FullName);
        Assert.Equal("CDL-90824-A", authState.CurrentSession.LicenseNumber);
        Assert.Equal("Bus #14", authState.CurrentSession.BusNumber);
        Assert.Equal("Morning Route 14-AM • Oakridge East", authState.CurrentSession.RouteName);
        Assert.Equal("InTransit", authState.CurrentSession.ShiftStatus);
        Assert.True(authState.IsInTransit);
        Assert.NotNull(authState.ActiveTripId);
    }

    [Fact]
    public void DriverAuthState_StartTrip_SetsStatusToInTransit()
    {
        // Arrange
        var authState = new DriverAuthState();
        bool eventFired = false;
        authState.OnChange += () => eventFired = true;

        // Act
        authState.StartTrip();

        // Assert
        Assert.True(eventFired);
        Assert.Equal("InTransit", authState.CurrentSession?.ShiftStatus);
        Assert.True(authState.IsInTransit);
    }

    [Fact]
    public void DriverAuthState_CompleteTrip_SetsStatusToCompleted()
    {
        // Arrange
        var authState = new DriverAuthState();
        bool eventFired = false;
        authState.OnChange += () => eventFired = true;

        // Act
        authState.CompleteTrip();

        // Assert
        Assert.True(eventFired);
        Assert.Equal("Completed", authState.CurrentSession?.ShiftStatus);
        Assert.False(authState.IsInTransit);
    }

    [Fact]
    public void DriverAuthState_Logout_ClearsSessionAndTripId()
    {
        // Arrange
        var authState = new DriverAuthState();
        Assert.True(authState.IsAuthenticated);

        // Act
        authState.Logout();

        // Assert
        Assert.False(authState.IsAuthenticated);
        Assert.Null(authState.CurrentSession);
        Assert.Null(authState.ActiveTripId);
    }

    [Fact]
    public async Task DriverApiClient_LoginAsync_ReturnsValidDriverSession()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);

        // Act
        var session = await client.LoginAsync("CDL-90824-A", "1234");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("Robert Jenkins", session.FullName);
        Assert.Equal("CDL-90824-A", session.LicenseNumber);
        Assert.Equal("Bus #14", session.BusNumber);
        Assert.Equal("InTransit", session.ShiftStatus);
    }

    [Fact]
    public async Task DriverApiClient_GetActiveRouteAsync_ReturnsTurnInstructionsAndStops()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var busId = authState.CurrentSession!.BusId;

        // Act
        var route = await client.GetActiveRouteAsync(busId);

        // Assert
        Assert.NotNull(route);
        Assert.Equal("Bus #14", route.BusNumber);
        Assert.Equal("Turn Left on Evergreen Blvd", route.NextTurnInstruction);
        Assert.Equal("300 Feet", route.DistanceToNextTurn);
        Assert.Equal(25, route.SpeedLimitKmh);
        Assert.True(route.RfidHardwareOnline);
        Assert.Equal(8, route.TotalStops);
        Assert.NotEmpty(route.Stops);
        Assert.Equal(24, route.OnboardCount);
        Assert.Equal(3, route.ToPickUpCount);
    }

    [Fact]
    public async Task DriverApiClient_GetStudentManifestAsync_ReturnsBoardedAndPendingStudents()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var tripId = authState.ActiveTripId!.Value;

        // Act
        var manifest = await client.GetStudentManifestAsync(tripId);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal(5, manifest.Count);

        var leo = manifest.FirstOrDefault(s => s.FullName == "Leo Martinez");
        Assert.NotNull(leo);
        Assert.True(leo.IsBoarded);
        Assert.Equal("4B-99-F2-10", leo.RfidCardUid);
        Assert.Equal("RfidScan", leo.BoardingMethod);

        var chloe = manifest.FirstOrDefault(s => s.FullName == "Chloe Davis");
        Assert.NotNull(chloe);
        Assert.True(chloe.IsAbsent);
        Assert.Contains("Doctor Appt", chloe.AbsenceReason);
    }

    [Fact]
    public async Task DriverApiClient_ManualCheckInStudentAsync_ReturnsTrue()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var tripId = authState.ActiveTripId!.Value;
        var studentId = Guid.NewGuid();

        // Act
        var result = await client.ManualCheckInStudentAsync(tripId, studentId, "Physical in-person verification by driver");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DriverApiClient_MarkStudentAbsentAsync_RecordsAbsence()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var tripId = authState.ActiveTripId!.Value;
        var studentId = Guid.NewGuid();

        // Act
        var result = await client.MarkStudentAbsentAsync(tripId, studentId, "No show at curbside after 2 minutes");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DriverApiClient_BroadcastDelayAsync_DeliversBroadcastSuccessfully()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var tripId = authState.ActiveTripId!.Value;

        var request = new BroadcastDelayRequest(
            MinutesDelay: 10,
            Reason: "Heavy Traffic",
            CustomNotes: "Highway 101 lane closure",
            BroadcastToParents: true,
            BroadcastToDispatch: true
        );

        // Act
        var result = await client.BroadcastDelayAsync(tripId, request);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DriverApiClient_SubmitPreTripChecklistAsync_RecordsVehicleSafetyCompliance()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);
        var busId = authState.CurrentSession!.BusId;

        var checklist = new DriverPreTripChecklist
        {
            BrakesAndTiresPassed = true,
            EmergencyExitsVerified = true,
            FirstAidKitStocked = true,
            RfidReaderTested = true,
            GpsTrackerTested = true,
            InteriorCleanSweepCompleted = true,
            Notes = "All 6 mandatory safety points inspected and signed off."
        };

        // Act
        var result = await client.SubmitPreTripChecklistAsync(busId, checklist);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DriverApiClient_TriggerEmergencySosAsync_TransmitsDistressSignal()
    {
        // Arrange
        var authState = new DriverAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new DriverApiClient(http, authState, NullLogger<DriverApiClient>.Instance);

        var request = new DriverSosAlertRequest(
            BusId: authState.CurrentSession!.BusId,
            BusNumber: authState.CurrentSession!.BusNumber,
            TripId: authState.ActiveTripId!.Value,
            Latitude: 37.7735m,
            Longitude: -122.4205m,
            Reason: "Medical emergency on bus route"
        );

        // Act
        var result = await client.TriggerEmergencySosAsync(request);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DriverNotificationService_ShowToast_DispatchesToastEvent()
    {
        // Arrange
        var service = new DriverNotificationService();
        DriverToastModel? received = null;
        service.OnToastReceived += t => received = t;

        // Act
        service.ShowToast("Door Scanner Active", "Leo Martinez scanned RFID badge.", "Success");

        // Assert
        Assert.NotNull(received);
        Assert.Equal("Door Scanner Active", received.Title);
        Assert.Equal("Leo Martinez scanned RFID badge.", received.Message);
        Assert.Equal("Success", received.Type);
        Assert.Single(service.ActiveToasts);
    }
}

