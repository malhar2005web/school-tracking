using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.ParentApp.Models;
using SchoolTrack.ParentApp.Services;
using Xunit;

namespace SchoolTrack.Api.Tests;

public class ParentApplicationTests
{
    [Fact]
    public void ParentAuthState_DefaultSession_InitializedWithSarahAndLeoMartinez()
    {
        // Arrange & Act
        var authState = new ParentAuthState();

        // Assert
        Assert.True(authState.IsAuthenticated);
        Assert.NotNull(authState.CurrentSession);
        Assert.Equal("Sarah Martinez", authState.CurrentSession.FullName);
        Assert.Equal("sarah.martinez@example.com", authState.CurrentSession.Email);
        Assert.Equal(2, authState.LinkedChildren.Count);

        Assert.NotNull(authState.ActiveChild);
        Assert.Equal("Leo Martinez", authState.ActiveChild.FullName);
        Assert.Equal("Grade 3", authState.ActiveChild.Grade);
        Assert.Equal("4B-99-F2-10", authState.ActiveChild.RfidCardUid);
        Assert.True(authState.ActiveChild.IsRfidCardActive);
        Assert.Equal("Bus #14", authState.ActiveChild.BusNumber);
        Assert.Equal("Robert Jenkins", authState.ActiveChild.PrimaryDriverName);
    }

    [Fact]
    public void ParentAuthState_SelectChild_SwitchesActiveChildToMaya()
    {
        // Arrange
        var authState = new ParentAuthState();
        var maya = authState.LinkedChildren.First(c => c.FullName == "Maya Martinez");
        bool eventFired = false;
        authState.OnChange += () => eventFired = true;

        // Act
        authState.SelectChild(maya.StudentId);

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(authState.ActiveChild);
        Assert.Equal("Maya Martinez", authState.ActiveChild.FullName);
        Assert.Equal("Grade 1", authState.ActiveChild.Grade);
        Assert.Equal("7C-12-E5-88", authState.ActiveChild.RfidCardUid);
    }

    [Fact]
    public void ParentAuthState_Logout_ClearsAuthenticationState()
    {
        // Arrange
        var authState = new ParentAuthState();
        Assert.True(authState.IsAuthenticated);

        // Act
        authState.Logout();

        // Assert
        Assert.False(authState.IsAuthenticated);
        Assert.Null(authState.CurrentSession);
        Assert.Null(authState.ActiveChild);
        Assert.Empty(authState.LinkedChildren);
    }

    [Fact]
    public async Task ParentApiClient_LoginWithPhone_ReturnsValidParentSession()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);

        // Act
        var session = await client.LoginWithPhoneAsync("+1 (555) 019-2834", "8421");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("Sarah Martinez", session.FullName);
        Assert.Equal("+1 (555) 019-2834", session.PhoneNumber);
        Assert.Equal(2, session.LinkedChildren.Count);
    }

    [Fact]
    public async Task ParentApiClient_GetChildActiveTrip_ReturnsValidWaypointsAndDriver()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);
        var leoId = authState.ActiveChild!.StudentId;

        // Act
        var trip = await client.GetChildActiveTripAsync(leoId);

        // Assert
        Assert.NotNull(trip);
        Assert.Equal("Bus #14", trip.BusNumber);
        Assert.Equal("Robert Jenkins", trip.DriverName);
        Assert.Equal("+1 (555) 329-8472", trip.DriverPhone);
        Assert.Equal(6, trip.Waypoints.Count);
        Assert.True(trip.Waypoints[0].IsCompleted);
        Assert.True(trip.Waypoints[0].IsChildStop);
        Assert.Equal("Oakridge & 5th Ave (Leo's Stop)", trip.Waypoints[0].StopName);
        Assert.Equal("Oakridge Elementary Campus", trip.Waypoints.Last().StopName);
        Assert.True(trip.IsLive);
        Assert.Equal(65, trip.CompletionPercentage);
    }

    [Fact]
    public async Task ParentApiClient_GetChildRfidLogs_ReturnsChronologicalMilestones()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);
        var leoId = authState.ActiveChild!.StudentId;

        // Act
        var logs = await client.GetChildRfidLogsAsync(leoId, DateTime.UtcNow);

        // Assert
        Assert.NotNull(logs);
        Assert.Equal(3, logs.Count);

        var boardingLog = logs.First(l => l.ScanType == "Boarding");
        Assert.Equal("07:42 AM", boardingLog.FormattedTime);
        Assert.Equal("Boarded Bus #14", boardingLog.MilestoneTitle);
        Assert.True(boardingLog.IsRfidVerified);
        Assert.NotNull(boardingLog.MapSnapshotUrl);

        var gateLog = logs.First(l => l.ScanType == "SchoolGate");
        Assert.Equal("08:05 AM", gateLog.FormattedTime);
        Assert.Equal("School Arrival & Gate Tap", gateLog.MilestoneTitle);
    }

    [Fact]
    public async Task ParentApiClient_ReportAbsence_RecordsSkipRequestSuccessfully()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);
        var leoId = authState.ActiveChild!.StudentId;

        var request = new AbsenceReportRequest(
            StudentId: leoId,
            Date: DateTime.UtcNow.Date,
            Reason: "Doctor",
            Details: "Dentist appointment at 9:00 AM",
            MorningTrip: true,
            AfternoonTrip: false
        );

        // Act
        var result = await client.ReportAbsenceAsync(request);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ParentApiClient_SafetySettings_ReadAndSave()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);
        var leoId = authState.ActiveChild!.StudentId;

        // Act
        var settings = await client.GetSafetySettingsAsync(leoId);
        settings.PushAlertsOnBoarding = false;
        var saveResult = await client.SaveSafetySettingsAsync(leoId, settings);

        // Assert
        Assert.NotNull(settings);
        Assert.True(saveResult);
    }

    [Fact]
    public void ParentNotificationService_ShowToast_DispatchesAndDismisses()
    {
        // Arrange
        var service = new ParentNotificationService();
        ParentNotificationModel? receivedNotification = null;
        service.OnNotificationReceived += notif => receivedNotification = notif;

        // Act - Show
        service.ShowToast("Test Boarding", "Child boarded bus", "Boarding", "Leo Martinez");

        // Assert - Show
        Assert.Single(service.ActiveToasts);
        Assert.NotNull(receivedNotification);
        Assert.Equal("Test Boarding", receivedNotification.Title);
        Assert.Equal("Leo Martinez", receivedNotification.ChildName);

        // Act - Dismiss
        service.DismissToast(receivedNotification.Id);

        // Assert - Dismiss
        Assert.Empty(service.ActiveToasts);
    }

    [Fact]
    public async Task ParentApiClient_GetAuthorizedGuardians_ReturnsApprovedList()
    {
        // Arrange
        var authState = new ParentAuthState();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ParentApiClient(http, authState, NullLogger<ParentApiClient>.Instance);
        var leoId = authState.ActiveChild!.StudentId;

        // Act
        var guardians = await client.GetAuthorizedGuardiansAsync(leoId);

        // Assert
        Assert.NotNull(guardians);
        Assert.Equal(3, guardians.Count);

        var primary = guardians.First(g => g.IsPrimary);
        Assert.Equal("Sarah Martinez", primary.FullName);
        Assert.True(primary.CanAuthorizePickup);

        var father = guardians.First(g => g.Relationship == "Father");
        Assert.Equal("David Martinez", father.FullName);
    }
}

