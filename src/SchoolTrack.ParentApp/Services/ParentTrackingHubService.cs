using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SchoolTrack.ParentApp.Models;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.ParentApp.Services;

public class ParentTrackingHubService : IAsyncDisposable
{
    private readonly ParentAuthState _authState;
    private readonly ILogger<ParentTrackingHubService> _logger;
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _simulatorCts;

    public event Action<LatestBusLocationDto>? OnBusLocationUpdated;
    public event Action<BoardingNotificationDto>? OnBoardingNotification;
    public event Action<TripStatusChangedDto>? OnTripStatusChanged;
    public event Action<TripStopStatusChangedDto>? OnTripStopStatusChanged;
    public event Action<BusConnectivityChangedDto>? OnBusConnectivityChanged;
    public event Action<bool>? OnConnectionStateChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public LatestBusLocationDto? CurrentLocation { get; private set; }

    public ParentTrackingHubService(ParentAuthState authState, ILogger<ParentTrackingHubService> logger)
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
                CurrentLocation = loc;
                OnBusLocationUpdated?.Invoke(loc);
            });

            _hubConnection.On<BoardingNotificationDto>("ReceiveBoardingNotification", notif =>
            {
                OnBoardingNotification?.Invoke(notif);
            });

            _hubConnection.On<TripStatusChangedDto>("ReceiveTripStatusChanged", status =>
            {
                OnTripStatusChanged?.Invoke(status);
            });

            _hubConnection.On<TripStopStatusChangedDto>("ReceiveTripStopStatusChanged", stop =>
            {
                OnTripStopStatusChanged?.Invoke(stop);
            });

            _hubConnection.On<BusConnectivityChangedDto>("ReceiveBusConnectivityChanged", conn =>
            {
                OnBusConnectivityChanged?.Invoke(conn);
            });

            _hubConnection.Reconnected += connectionId =>
            {
                OnConnectionStateChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                OnConnectionStateChanged?.Invoke(false);
                StartSimulation();
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            OnConnectionStateChanged?.Invoke(true);

            // Join the active child's bus group
            if (_authState.ActiveChild != null)
            {
                await _hubConnection.InvokeAsync("JoinBusGroup", _authState.ActiveChild.BusId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live SignalR hub connection failed. Activating local telemetry simulation.");
            OnConnectionStateChanged?.Invoke(false);
            StartSimulation();
        }
    }

    public void StartSimulation()
    {
        if (_simulatorCts != null) return;

        _simulatorCts = new CancellationTokenSource();
        var token = _simulatorCts.Token;

        Task.Run(async () =>
        {
            var waypoints = new (decimal Lat, decimal Lng)[]
            {
                (37.7720m, -122.4220m),
                (37.7735m, -122.4205m),
                (37.7749m, -122.4194m),
                (37.7765m, -122.4178m),
                (37.7780m, -122.4160m),
                (37.7795m, -122.4140m)
            };

            int index = 2;
            decimal lat = waypoints[index].Lat;
            decimal lng = waypoints[index].Lng;
            var rand = new Random();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3500, token);

                    var nextIndex = (index + 1) % waypoints.Length;
                    var target = waypoints[nextIndex];

                    lat += (target.Lat - lat) * 0.15m;
                    lng += (target.Lng - lng) * 0.15m;

                    if (Math.Abs(lat - target.Lat) < 0.0002m && Math.Abs(lng - target.Lng) < 0.0002m)
                    {
                        index = nextIndex;
                    }

                    var speed = (decimal)(24.0 + rand.NextDouble() * 8.0);
                    var heading = 42.0m + (decimal)(rand.NextDouble() * 10.0 - 5.0);

                    CurrentLocation = new LatestBusLocationDto(
                        BusId: _authState.ActiveChild?.BusId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        BusNumber: "Bus #14",
                        SchoolId: _authState.CurrentSession?.SchoolId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        TripId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        TripStatus: "InProgress",
                        Latitude: lat,
                        Longitude: lng,
                        SpeedKmh: speed,
                        Heading: heading,
                        Altitude: 45m,
                        AccuracyMeters: 4.2m,
                        IgnitionOn: true,
                        IsOnline: true,
                        RecordedAt: DateTimeOffset.UtcNow,
                        UpdatedAt: DateTimeOffset.UtcNow
                    );

                    OnBusLocationUpdated?.Invoke(CurrentLocation);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in parent tracking simulation");
                }
            }
        }, token);
    }

    public async ValueTask DisposeAsync()
    {
        _simulatorCts?.Cancel();
        _simulatorCts?.Dispose();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
