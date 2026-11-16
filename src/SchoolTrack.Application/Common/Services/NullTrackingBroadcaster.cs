using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Application.Common.Services;

/// <summary>
/// No-op implementation of <see cref="ITrackingBroadcaster"/> used as a safe fallback in headless or unit-testing environments.
/// </summary>
public sealed class NullTrackingBroadcaster : ITrackingBroadcaster
{
    public Task BroadcastBusLocationAsync(LatestBusLocationDto location, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task BroadcastBoardingNotificationAsync(BoardingNotificationDto notification, IReadOnlyList<Guid> parentUserIds, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task BroadcastTripStatusChangedAsync(TripStatusChangedDto statusEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task BroadcastTripStopStatusChangedAsync(TripStopStatusChangedDto stopEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task BroadcastBusConnectivityChangedAsync(BusConnectivityChangedDto connectivityEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

