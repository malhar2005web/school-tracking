using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Api.Tests;

public sealed class TestTenant(Guid? schoolId, bool isPlatformAdmin = false) : ITenantContext
{
    public Guid? SchoolId => schoolId;
    public bool IsPlatformAdmin => isPlatformAdmin;
}

public sealed class TestCurrentUser(
    Guid? userId = null,
    Guid? schoolId = null,
    string? role = null,
    string? email = null,
    bool isAuthenticated = true) : ICurrentUser
{
    public Guid? UserId => userId;
    public Guid? SchoolId => schoolId;
    public string? Email => email ?? (userId.HasValue ? $"user_{userId:N}@test.local" : null);
    public string? Role => role;
    public bool IsAuthenticated => isAuthenticated;
}

public sealed class TestTrackingBroadcaster : ITrackingBroadcaster
{
    public List<LatestBusLocationDto> BroadcastedLocations { get; } = new();
    public List<(BoardingNotificationDto Notification, IReadOnlyList<Guid> Parents)> BroadcastedBoardingAlerts { get; } = new();
    public List<TripStatusChangedDto> BroadcastedTripStatusEvents { get; } = new();
    public List<TripStopStatusChangedDto> BroadcastedStopStatusEvents { get; } = new();
    public List<BusConnectivityChangedDto> BroadcastedConnectivityEvents { get; } = new();

    public Task BroadcastBusLocationAsync(LatestBusLocationDto location, CancellationToken cancellationToken = default)
    {
        BroadcastedLocations.Add(location);
        return Task.CompletedTask;
    }

    public Task BroadcastBoardingNotificationAsync(BoardingNotificationDto notification, IReadOnlyList<Guid> parentUserIds, CancellationToken cancellationToken = default)
    {
        BroadcastedBoardingAlerts.Add((notification, parentUserIds));
        return Task.CompletedTask;
    }

    public Task BroadcastTripStatusChangedAsync(TripStatusChangedDto statusEvent, CancellationToken cancellationToken = default)
    {
        BroadcastedTripStatusEvents.Add(statusEvent);
        return Task.CompletedTask;
    }

    public Task BroadcastTripStopStatusChangedAsync(TripStopStatusChangedDto stopEvent, CancellationToken cancellationToken = default)
    {
        BroadcastedStopStatusEvents.Add(stopEvent);
        return Task.CompletedTask;
    }

    public Task BroadcastBusConnectivityChangedAsync(BusConnectivityChangedDto connectivityEvent, CancellationToken cancellationToken = default)
    {
        BroadcastedConnectivityEvents.Add(connectivityEvent);
        return Task.CompletedTask;
    }
}


