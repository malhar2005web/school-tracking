using Microsoft.AspNetCore.SignalR.Client;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Admin.Services;

public class LiveFleetTrackingService : IAsyncDisposable
{
    private readonly AdminAuthStateProvider _authState;
    private readonly IConfiguration _config;
    private readonly ILogger<LiveFleetTrackingService> _logger;
    private HubConnection? _hubConnection;

    public event Action<LatestBusLocationDto>? OnBusLocationReceived;
    public event Action<BoardingNotificationDto>? OnBoardingNotificationReceived;
    public event Action<TripStatusChangedDto>? OnTripStatusChanged;
    public event Action<TripStopStatusChangedDto>? OnTripStopStatusChanged;
    public event Action<BusConnectivityChangedDto>? OnBusConnectivityChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public LiveFleetTrackingService(AdminAuthStateProvider authState, IConfiguration config, ILogger<LiveFleetTrackingService> logger)
    {
        _authState = authState;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        var baseUrl = _config["Api:BaseUrl"] ?? "http://localhost:5000";
        var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/tracking";

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_authState.CurrentSession.AccessToken);
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            _hubConnection.On<LatestBusLocationDto>("ReceiveBusLocation", dto =>
            {
                OnBusLocationReceived?.Invoke(dto);
            });

            _hubConnection.On<BoardingNotificationDto>("ReceiveBoardingNotification", dto =>
            {
                OnBoardingNotificationReceived?.Invoke(dto);
            });

            _hubConnection.On<TripStatusChangedDto>("ReceiveTripStatusChanged", dto =>
            {
                OnTripStatusChanged?.Invoke(dto);
            });

            _hubConnection.On<TripStopStatusChangedDto>("ReceiveTripStopStatusChanged", dto =>
            {
                OnTripStopStatusChanged?.Invoke(dto);
            });

            _hubConnection.On<BusConnectivityChangedDto>("ReceiveBusConnectivityChanged", dto =>
            {
                OnBusConnectivityChanged?.Invoke(dto);
            });

            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to SignalR Tracking Hub at {HubUrl}", hubUrl);

            // Join school channel
            var schoolId = _authState.CurrentSession.SchoolId;
            if (schoolId != Guid.Empty)
            {
                await _hubConnection.InvokeAsync("JoinSchoolGroup", schoolId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SignalR tracking hub connection could not be established: {Message}. Operating in offline/simulated mode.", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
