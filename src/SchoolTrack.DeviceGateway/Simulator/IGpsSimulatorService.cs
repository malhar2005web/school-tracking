using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Simulator;

public interface IGpsSimulatorService
{
    Task<SimulateGpsTripResponse> SimulateTripAsync(SimulateGpsTripRequest request, CancellationToken cancellationToken = default);
}

public sealed class GpsSimulatorService : IGpsSimulatorService
{
    private readonly IApplicationDbContext _context;
    private readonly IGpsIngestionService _ingestionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<GpsSimulatorService> _logger;

    public GpsSimulatorService(
        IApplicationDbContext context,
        IGpsIngestionService ingestionService,
        IDateTimeProvider dateTimeProvider,
        ILogger<GpsSimulatorService> logger)
    {
        _context = context;
        _ingestionService = ingestionService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<SimulateGpsTripResponse> SimulateTripAsync(SimulateGpsTripRequest request, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Route)
                .ThenInclude(r => r!.Stops.OrderBy(s => s.SequenceOrder))
            .Include(t => t.Bus)
                .ThenInclude(b => b!.GpsDevice)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
        {
            throw new KeyNotFoundException($"Trip with ID '{request.TripId}' was not found.");
        }

        if (trip.Bus == null || trip.Bus.GpsDevice == null)
        {
            throw new InvalidOperationException($"Bus assigned to trip '{request.TripId}' has no registered GPS device.");
        }

        var stops = trip.Route?.Stops.OrderBy(s => s.SequenceOrder).ToList() ?? new List<RouteStop>();
        if (stops.Count == 0)
        {
            throw new InvalidOperationException($"Route on trip '{request.TripId}' has no stops defined.");
        }

        var device = trip.Bus.GpsDevice;
        var stepSeconds = Math.Max(1, request.StepSeconds);
        var averageSpeedKmh = Math.Clamp(request.AverageSpeedKmh, 10m, 80m);
        var speedMps = (double)averageSpeedKmh * (1000.0 / 3600.0);

        var currentSimTime = _dateTimeProvider.UtcNow.AddMinutes(-stops.Count * 5);
        int pointsIngested = 0;

        if (stops.Count == 1)
        {
            // Single stop: generate stationary packets
            for (int i = 0; i < 5; i++)
            {
                var packet = new GpsTelemetryPacket(
                    device.DeviceCode,
                    device.ApiKey,
                    device.Imei,
                    stops[0].Latitude,
                    stops[0].Longitude,
                    0m,
                    0m,
                    100m,
                    2.5m,
                    true,
                    currentSimTime,
                    "{\"simulator\":true}");

                await _ingestionService.IngestAsync(packet, cancellationToken);
                pointsIngested++;
                currentSimTime = currentSimTime.AddSeconds(stepSeconds);
            }

            return new SimulateGpsTripResponse(
                trip.Id,
                trip.BusId,
                trip.Route?.Name ?? "Unknown",
                5,
                pointsIngested,
                TimeSpan.FromSeconds(5 * stepSeconds));
        }

        // Multi-stop route simulation: interpolate between consecutive waypoints
        var startTime = currentSimTime;

        for (int i = 0; i < stops.Count - 1; i++)
        {
            var stopA = stops[i];
            var stopB = stops[i + 1];

            var distanceMeters = HaversineDistanceCalculator.CalculateDistanceMeters(
                stopA.Latitude, stopA.Longitude, stopB.Latitude, stopB.Longitude);

            var heading = HaversineDistanceCalculator.CalculateHeadingDegrees(
                stopA.Latitude, stopA.Longitude, stopB.Latitude, stopB.Longitude);

            var travelTimeSeconds = Math.Max(stepSeconds, (int)Math.Round(distanceMeters / speedMps));
            var steps = Math.Max(1, travelTimeSeconds / stepSeconds);

            for (int step = 0; step <= steps; step++)
            {
                var fraction = (decimal)step / steps;
                var lat = stopA.Latitude + (stopB.Latitude - stopA.Latitude) * fraction;
                var lon = stopA.Longitude + (stopB.Longitude - stopA.Longitude) * fraction;
                var speed = (step == 0 || step == steps) ? 0m : averageSpeedKmh;

                var packet = new GpsTelemetryPacket(
                    device.DeviceCode,
                    device.ApiKey,
                    device.Imei,
                    Math.Round(lat, 7),
                    Math.Round(lon, 7),
                    speed,
                    Math.Round(heading, 2),
                    120m,
                    3.0m,
                    true,
                    currentSimTime,
                    "{\"simulator\":true}");

                await _ingestionService.IngestAsync(packet, cancellationToken);
                pointsIngested++;
                currentSimTime = currentSimTime.AddSeconds(stepSeconds);
            }

            // Dwell time at stop: 30 seconds idle
            currentSimTime = currentSimTime.AddSeconds(30);
        }

        var totalElapsed = currentSimTime - startTime;

        _logger.LogInformation("Completed GPS simulation for Trip {TripId}: {Count} points ingested.", trip.Id, pointsIngested);

        return new SimulateGpsTripResponse(
            trip.Id,
            trip.BusId,
            trip.Route?.Name ?? "Unknown",
            pointsIngested,
            pointsIngested,
            totalElapsed);
    }
}

