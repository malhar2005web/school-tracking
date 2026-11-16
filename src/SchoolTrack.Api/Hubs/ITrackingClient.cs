using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Api.Hubs;

/// <summary>
/// Strongly typed SignalR client invocations received by tracking frontends (Blazor Admin, Parent App, Driver App).
/// </summary>
public interface ITrackingClient
{
    Task ReceiveBusLocation(LatestBusLocationDto location);

    Task ReceiveBoardingNotification(BoardingNotificationDto notification);

    Task ReceiveTripStatusChanged(TripStatusChangedDto statusEvent);

    Task ReceiveTripStopStatusChanged(TripStopStatusChangedDto stopEvent);

    Task ReceiveBusConnectivityChanged(BusConnectivityChangedDto connectivityEvent);
}

