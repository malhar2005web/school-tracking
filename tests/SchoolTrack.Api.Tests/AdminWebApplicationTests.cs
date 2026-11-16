using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Admin.Models;
using SchoolTrack.Admin.Services;

namespace SchoolTrack.Api.Tests;

public class AdminWebApplicationTests
{
    [Fact]
    public async Task AdminAuthStateProvider_DefaultSession_IsAuthenticatedWithElenaVasquez()
    {
        // Arrange
        var provider = new AdminAuthStateProvider();

        // Act
        var authState = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.NotNull(authState.User.Identity);
        Assert.True(authState.User.Identity.IsAuthenticated);
        Assert.Equal("Elena Vasquez", authState.User.Identity.Name);
        Assert.Equal("SchoolAdmin", provider.CurrentSession.Role);
        Assert.Equal("Oakridge Academy District #4", provider.CurrentSession.SchoolName);
    }

    [Fact]
    public async Task AdminAuthStateProvider_Logout_ClearsAuthentication()
    {
        // Arrange
        var provider = new AdminAuthStateProvider();
        Assert.True(provider.CurrentSession.IsAuthenticated);

        // Act
        provider.Logout();
        var authState = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.False(provider.CurrentSession.IsAuthenticated);
        Assert.False(authState.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task AdminAuthStateProvider_SetSession_UpdatesClaimsAndSchoolContext()
    {
        // Arrange
        var provider = new AdminAuthStateProvider();
        var customSchoolId = Guid.NewGuid();
        var newSession = new AuthSession
        {
            AccessToken = "test-token-123",
            RefreshToken = "test-refresh-123",
            FullName = "Marcus Superintendent",
            Email = "marcus@district.edu",
            Role = "SystemAdmin",
            SchoolId = customSchoolId,
            SchoolName = "Northview Unified School District"
        };

        // Act
        provider.SetSession(newSession);
        var authState = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.True(provider.CurrentSession.IsAuthenticated);
        Assert.Equal("Marcus Superintendent", authState.User.Identity?.Name);
        Assert.Equal(customSchoolId, provider.CurrentSession.SchoolId);
        Assert.Equal("Northview Unified School District", provider.CurrentSession.SchoolName);
    }

    [Fact]
    public async Task ApiClient_GetDashboardSummary_ReturnsValidFleetAndStudentMetrics()
    {
        // Arrange
        var authState = new AdminAuthStateProvider();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ApiClient(http, authState, NullLogger<ApiClient>.Instance);

        // Act
        var summary = await client.GetDashboardSummaryAsync(authState.CurrentSession.SchoolId);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(18, summary.ActiveFleetCount);
        Assert.Equal(20, summary.TotalFleetCount);
        Assert.Equal(412, summary.StudentsBoardedCount);
        Assert.Equal(438, summary.TotalStudentsCount);
        Assert.Equal(24, summary.GatewaysOnlineCount);
        Assert.NotEmpty(summary.RecentTaps);
        Assert.NotEmpty(summary.ActiveBuses);
    }

    [Fact]
    public async Task ApiClient_GetFleetRadar_ReturnsMappedBusesWithGpsAndDrivers()
    {
        // Arrange
        var authState = new AdminAuthStateProvider();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ApiClient(http, authState, NullLogger<ApiClient>.Instance);

        // Act
        var radar = await client.GetFleetRadarAsync(authState.CurrentSession.SchoolId);

        // Assert
        Assert.NotNull(radar);
        Assert.True(radar.Count >= 4);

        var bus14 = radar.FirstOrDefault(b => b.BusNumber == "Bus #14");
        Assert.NotNull(bus14);
        Assert.Equal("Robert Jenkins", bus14.DriverName);
        Assert.True(bus14.SpeedMph > 0);
        Assert.True(bus14.Latitude > 0);
        Assert.True(bus14.Longitude < 0);
        Assert.Equal("Route Westview A", bus14.RouteName);
    }

    [Fact]
    public async Task ApiClient_GetStudents_FiltersByGradeAndSearchTerm()
    {
        // Arrange
        var authState = new AdminAuthStateProvider();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ApiClient(http, authState, NullLogger<ApiClient>.Instance);

        // Act - All
        var allStudents = await client.GetStudentsAsync(authState.CurrentSession.SchoolId);
        Assert.True(allStudents.Count >= 8);

        // Act - Filter Grade 4
        var grade4Students = await client.GetStudentsAsync(authState.CurrentSession.SchoolId, grade: "Grade 4");
        Assert.All(grade4Students, s => Assert.Contains("Grade 4", s.ClassName));

        // Act - Search by name
        var leoStudents = await client.GetStudentsAsync(authState.CurrentSession.SchoolId, search: "Leo");
        Assert.Single(leoStudents);
        Assert.Equal("Leo", leoStudents[0].FirstName);
    }

    [Fact]
    public async Task ApiClient_IssueTempRfidCard_Succeeds()
    {
        // Arrange
        var authState = new AdminAuthStateProvider();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ApiClient(http, authState, NullLogger<ApiClient>.Instance);
        var studentId = Guid.NewGuid();

        // Act
        var success = await client.IssueTempRfidCardAsync(authState.CurrentSession.SchoolId, studentId, "ST-TEMP-9999");

        // Assert
        Assert.True(success);
    }

    [Fact]
    public async Task ApiClient_GetRoutes_ReturnsOrderedStopsWithGeofence()
    {
        // Arrange
        var authState = new AdminAuthStateProvider();
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var client = new ApiClient(http, authState, NullLogger<ApiClient>.Instance);

        // Act
        var routes = await client.GetRoutesAsync(authState.CurrentSession.SchoolId);

        // Assert
        Assert.NotEmpty(routes);
        var westview = routes.FirstOrDefault(r => r.Name == "Route Westview A");
        Assert.NotNull(westview);
        Assert.True(westview.Stops.Count >= 5);
        Assert.All(westview.Stops, s => Assert.True(s.GeofenceRadiusMeters > 0));
    }
}

