using Microsoft.AspNetCore.SignalR;
using SchoolTrack.Api.Hubs;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Api.Services;

/// <summary>
/// Dispatches real-time events to connected SignalR clients and tenant/trip/parent group channels.
/// </summary>
public sealed class SignalRTrackingBroadcaster : ITrackingBroadcaster
{
    private readonly IHubContext<TrackingHub, ITrackingClient> _hubContext;
    private readonly ILogger<SignalRTrackingBroadcaster> _logger;

    public SignalRTrackingBroadcaster(
        IHubContext<TrackingHub, ITrackingClient> hubContext,
        ILogger<SignalRTrackingBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastBusLocationAsync(LatestBusLocationDto location, CancellationToken cancellationToken = default)
    {
        var busGroup = TrackingHub.BusGroup(location.BusId);
        var schoolGroup = TrackingHub.SchoolGroup(location.SchoolId);

        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(busGroup).ReceiveBusLocation(location),
            _hubContext.Clients.Group(schoolGroup).ReceiveBusLocation(location)
        };

        if (location.TripId.HasValue)
        {
            var tripGroup = TrackingHub.TripGroup(location.TripId.Value);
            tasks.Add(_hubContext.Clients.Group(tripGroup).ReceiveBusLocation(location));
        }

        await Task.WhenAll(tasks);
        _logger.LogDebug("Broadcasted location for Bus {BusId} to groups", location.BusId);
    }

    public async Task BroadcastBoardingNotificationAsync(
        BoardingNotificationDto notification,
        IReadOnlyList<Guid> parentUserIds,
        CancellationToken cancellationToken = default)
    {
        var schoolGroup = TrackingHub.SchoolGroup(notification.SchoolId);
        var tripGroup = TrackingHub.TripGroup(notification.TripId);

        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(schoolGroup).ReceiveBoardingNotification(notification),
            _hubContext.Clients.Group(tripGroup).ReceiveBoardingNotification(notification)
        };

        // Notify each linked parent via their private channel
        foreach (var parentUserId in parentUserIds)
        {
            var parentGroup = TrackingHub.ParentGroup(parentUserId);
            tasks.Add(_hubContext.Clients.Group(parentGroup).ReceiveBoardingNotification(notification));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Broadcasted {ScanType} alert for Student {StudentId} to {Count} parents and trip group.",
            notification.ScanType, notification.StudentId, parentUserIds.Count);
    }

    public async Task BroadcastTripStatusChangedAsync(TripStatusChangedDto statusEvent, CancellationToken cancellationToken = default)
    {
        var schoolGroup = TrackingHub.SchoolGroup(statusEvent.SchoolId);
        var tripGroup = TrackingHub.TripGroup(statusEvent.TripId);
        var busGroup = TrackingHub.BusGroup(statusEvent.BusId);

        await Task.WhenAll(
            _hubContext.Clients.Group(schoolGroup).ReceiveTripStatusChanged(statusEvent),
            _hubContext.Clients.Group(tripGroup).ReceiveTripStatusChanged(statusEvent),
            _hubContext.Clients.Group(busGroup).ReceiveTripStatusChanged(statusEvent));

        _logger.LogInformation("Broadcasted trip status transition {From} -> {To} for Trip {TripId}",
            statusEvent.PreviousStatus, statusEvent.NewStatus, statusEvent.TripId);
    }

    public async Task BroadcastTripStopStatusChangedAsync(TripStopStatusChangedDto stopEvent, CancellationToken cancellationToken = default)
    {
        var schoolGroup = TrackingHub.SchoolGroup(stopEvent.SchoolId);
        var tripGroup = TrackingHub.TripGroup(stopEvent.TripId);

        await Task.WhenAll(
            _hubContext.Clients.Group(schoolGroup).ReceiveTripStopStatusChanged(stopEvent),
            _hubContext.Clients.Group(tripGroup).ReceiveTripStopStatusChanged(stopEvent));

        _logger.LogInformation("Broadcasted stop status {Status} for Stop '{StopName}' on Trip {TripId}",
            stopEvent.Status, stopEvent.StopName, stopEvent.TripId);
    }

    public async Task BroadcastBusConnectivityChangedAsync(BusConnectivityChangedDto connectivityEvent, CancellationToken cancellationToken = default)
    {
        var schoolGroup = TrackingHub.SchoolGroup(connectivityEvent.SchoolId);
        var busGroup = TrackingHub.BusGroup(connectivityEvent.BusId);

        await Task.WhenAll(
            _hubContext.Clients.Group(schoolGroup).ReceiveBusConnectivityChanged(connectivityEvent),
            _hubContext.Clients.Group(busGroup).ReceiveBusConnectivityChanged(connectivityEvent));

        _logger.LogInformation("Broadcasted bus connectivity changed for Bus {BusId}: Online={IsOnline}",
            connectivityEvent.BusId, connectivityEvent.IsOnline);
    }
}

