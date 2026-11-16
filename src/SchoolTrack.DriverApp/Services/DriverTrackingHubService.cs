using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SchoolTrack.DriverApp.Models;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.DriverApp.Services;

public class DriverTrackingHubService : IAsyncDisposable
{
    private readonly DriverAuthState _authState;
    private readonly ILogger<DriverTrackingHubService> _logger;
    private HubConnection? _hubConnection;

    public event Action<BoardingNotificationDto>? OnStudentBoardedViaRfid;
    public event Action<TripStatusChangedDto>? OnTripStatusChanged;
    public event Action<bool>? OnConnectionStateChanged;
    public event Action<decimal, decimal>? OnSpeedHeadingUpdated;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public DriverTrackingHubService(DriverAuthState authState, ILogger<DriverTrackingHubService> logger)
    {
        _authState = authState;
        _logger = logger;
    }

    public async Task StartAsync(string hubUrl = "http://localhost:5000/hubs/tracking")
    {
        try
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_authState.CurrentSession?.AccessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<LatestBusLocationDto>("ReceiveBusLocation", loc =>
            {
                OnSpeedHeadingUpdated?.Invoke(loc.SpeedKmh, loc.Heading);
            });

            _hubConnection.On<BoardingNotificationDto>("ReceiveBoardingNotification", notif =>
            {
                OnStudentBoardedViaRfid?.Invoke(notif);
            });

            _hubConnection.On<TripStatusChangedDto>("ReceiveTripStatusChanged", status =>
            {
                OnTripStatusChanged?.Invoke(status);
            });

            _hubConnection.Reconnected += connectionId =>
            {
                OnConnectionStateChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                OnConnectionStateChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            OnConnectionStateChanged?.Invoke(true);

            if (_authState.CurrentSession != null)
            {
                await _hubConnection.InvokeAsync("JoinBusGroup", _authState.CurrentSession.BusId);
                if (_authState.ActiveTripId.HasValue)
                {
                    await _hubConnection.InvokeAsync("JoinTripGroup", _authState.ActiveTripId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Driver SignalR hub connection failed. Operating in local in-cab mode.");
            OnConnectionStateChanged?.Invoke(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
