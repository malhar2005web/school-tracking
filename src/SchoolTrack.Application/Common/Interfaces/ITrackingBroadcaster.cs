using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Application.Common.Interfaces;

/// <summary>
/// Dispatches real-time telemetry, boarding alerts, and trip status updates to connected SignalR groups and client devices.
/// </summary>
public interface ITrackingBroadcaster
{
    Task BroadcastBusLocationAsync(LatestBusLocationDto location, CancellationToken cancellationToken = default);

    Task BroadcastBoardingNotificationAsync(BoardingNotificationDto notification, IReadOnlyList<Guid> parentUserIds, CancellationToken cancellationToken = default);

    Task BroadcastTripStatusChangedAsync(TripStatusChangedDto statusEvent, CancellationToken cancellationToken = default);

    Task BroadcastTripStopStatusChangedAsync(TripStopStatusChangedDto stopEvent, CancellationToken cancellationToken = default);

    Task BroadcastBusConnectivityChangedAsync(BusConnectivityChangedDto connectivityEvent, CancellationToken cancellationToken = default);
}

