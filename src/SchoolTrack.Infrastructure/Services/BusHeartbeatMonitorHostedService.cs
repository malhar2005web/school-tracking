using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.RealTime;

namespace SchoolTrack.Infrastructure.Services;

/// <summary>
/// Background watchdog service monitoring bus telematics freshness and broadcasting offline events when heartbeats cease.
/// </summary>
public sealed class BusHeartbeatMonitorHostedService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(90);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BusHeartbeatMonitorHostedService> _logger;

    public BusHeartbeatMonitorHostedService(
        IServiceScopeFactory scopeFactory,
        IDateTimeProvider dateTimeProvider,
        ILogger<BusHeartbeatMonitorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bus Heartbeat Monitor Watchdog started.");

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckStaleBusesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during bus heartbeat evaluation cycle.");
            }
        }

        _logger.LogInformation("Bus Heartbeat Monitor Watchdog stopped.");
    }

    public async Task CheckStaleBusesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<ITrackingBroadcaster>();

        var utcNow = _dateTimeProvider.UtcNow;
        var thresholdTime = utcNow - OfflineThreshold;

        var staleLocations = await context.LatestBusLocations
            .IgnoreQueryFilters()
            .Include(l => l.Bus)
                .ThenInclude(b => b!.GpsDevice)
            .Where(l => l.IsOnline && l.RecordedAt < thresholdTime)
            .ToListAsync(cancellationToken);

        if (staleLocations.Count == 0)
        {
            return;
        }

        foreach (var location in staleLocations)
        {
            location.IsOnline = false;

            if (location.Bus?.GpsDevice != null)
            {
                location.Bus.GpsDevice.Status = "Offline";
            }

            var busNumber = location.Bus?.BusNumber ?? "Unknown";

            await broadcaster.BroadcastBusConnectivityChangedAsync(
                new BusConnectivityChangedDto(
                    location.SchoolId,
                    location.BusId,
                    busNumber,
                    false,
                    utcNow),
                cancellationToken);

            _logger.LogWarning("Bus {BusNumber} (ID: {BusId}) flagged OFFLINE. Last telemetry at {RecordedAt}.",
                busNumber, location.BusId, location.RecordedAt);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

