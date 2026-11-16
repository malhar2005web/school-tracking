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

public interface IAdvancedGpsSimulator
{
    Task<AdvancedGpsSimulationResult> RunSimulationAsync(
        AdvancedGpsSimulationOptions options,
        CancellationToken cancellationToken = default);
}

public sealed class AdvancedGpsSimulator : IAdvancedGpsSimulator
{
    private readonly IApplicationDbContext _context;
    private readonly IGpsIngestionService _ingestionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AdvancedGpsSimulator> _logger;

    public AdvancedGpsSimulator(
        IApplicationDbContext context,
        IGpsIngestionService ingestionService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AdvancedGpsSimulator> logger)
    {
        _context = context;
        _ingestionService = ingestionService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<AdvancedGpsSimulationResult> RunSimulationAsync(
        AdvancedGpsSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Route)
                .ThenInclude(r => r!.Stops.OrderBy(s => s.SequenceOrder))
            .Include(t => t.Bus)
                .ThenInclude(b => b!.GpsDevice)
            .FirstOrDefaultAsync(t => t.Id == options.TripId, cancellationToken);

        if (trip == null)
            throw new KeyNotFoundException($"Trip with ID '{options.TripId}' was not found.");

        if (trip.Bus?.GpsDevice == null)
            throw new InvalidOperationException($"Bus assigned to trip '{options.TripId}' has no active registered GPS device.");

        var stops = trip.Route?.Stops.OrderBy(s => s.SequenceOrder).ToList() ?? new List<RouteStop>();
        if (stops.Count == 0)
            throw new InvalidOperationException($"Route on trip '{options.TripId}' contains zero stops.");

        var device = trip.Bus.GpsDevice;
        var logEvents = new List<string>();
        var generatedPackets = new List<GpsTelemetryPacket>();
        var offlineQueue = new List<GpsTelemetryPointDto>();

        int totalPacketsIngested = 0;
        int offlineBatchedPackets = 0;
        int geofenceArrivalTriggers = 0;

        var simTime = _dateTimeProvider.UtcNow.AddMinutes(-stops.Count * 8);
        var startTime = simTime;

        logEvents.Add($"[SIM_INIT] Initialized kinematics simulation for Trip '{trip.Id}', Bus '{trip.Bus.BusNumber}', Device '{device.DeviceCode}'.");

        int currentStep = 0;

        // Iterate through consecutive stop segments
        for (int i = 0; i < stops.Count; i++)
        {
            var currentStop = stops[i];
            logEvents.Add($"[STOP_ARRIVED] Bus arrived at Stop {currentStop.SequenceOrder}: '{currentStop.StopName}' ({currentStop.Latitude}, {currentStop.Longitude}).");
            geofenceArrivalTriggers++;

            // Dwell time at the stop (generating stationary packets)
            int dwellSteps = Math.Max(1, options.StopDwellSeconds / options.StepSeconds);
            for (int d = 0; d < dwellSteps; d++)
            {
                currentStep++;
                simTime = simTime.AddSeconds(options.StepSeconds);

                var dwellPacket = CreatePacket(
                    device,
                    currentStop.Latitude,
                    currentStop.Longitude,
                    0m, // stationary
                    0m,
                    accuracy: 0.8m,
                    ignition: true,
                    simTime,
                    $"{{ \"dwell\": true, \"stopSequence\": {currentStop.SequenceOrder} }}"
                );

                generatedPackets.Add(dwellPacket);
                offlineBatchedPackets += await DispatchPacketAsync(dwellPacket, currentStep, options, offlineQueue, logEvents, cancellationToken);
                totalPacketsIngested++;
            }

            // If this is the final stop, break
            if (i >= stops.Count - 1)
                break;

            var nextStop = stops[i + 1];
            logEvents.Add($"[TRANSIT] Departing Stop {currentStop.SequenceOrder} for Stop {nextStop.SequenceOrder}: '{nextStop.StopName}'.");

            // Calculate distance & intermediate points
            var distanceMeters = HaversineDistanceCalculator.CalculateDistanceMeters(
                currentStop.Latitude, currentStop.Longitude,
                nextStop.Latitude, nextStop.Longitude);

            var bearing = CalculateBearing(
                (double)currentStop.Latitude, (double)currentStop.Longitude,
                (double)nextStop.Latitude, (double)nextStop.Longitude);

            // Compute number of transit steps based on cruise speed
            var cruiseSpeedMps = (double)options.CruiseSpeedKmh * (1000.0 / 3600.0);
            var estimatedTravelSeconds = Math.Max(options.StepSeconds * 2, (int)(distanceMeters / Math.Max(5.0, cruiseSpeedMps)));
            int segmentSteps = Math.Max(2, estimatedTravelSeconds / options.StepSeconds);

            for (int s = 1; s <= segmentSteps; s++)
            {
                currentStep++;
                simTime = simTime.AddSeconds(options.StepSeconds);

                double fraction = (double)s / segmentSteps;
                var lat = currentStop.Latitude + (decimal)fraction * (nextStop.Latitude - currentStop.Latitude);
                var lon = currentStop.Longitude + (decimal)fraction * (nextStop.Longitude - currentStop.Longitude);

                // Speed trapezoid: accelerate, cruise, decelerate
                decimal currentSpeed;
                if (fraction < 0.25)
                    currentSpeed = options.CruiseSpeedKmh * (decimal)(fraction / 0.25);
                else if (fraction > 0.75)
                    currentSpeed = options.CruiseSpeedKmh * (decimal)((1.0 - fraction) / 0.25);
                else
                    currentSpeed = options.CruiseSpeedKmh;

                // Check degraded accuracy simulation
                decimal accuracy = 0.8m;
                if (options.SimulateDegradedGps &&
                    currentStep >= options.DegradedStartStep &&
                    currentStep < options.DegradedStartStep + options.DegradedDurationSteps)
                {
                    accuracy = 24.5m; // degraded satellite geometry (high HDOP)
                    lat += 0.00015m;
                    lon -= 0.00010m;
                    logEvents.Add($"[GPS_DEGRADED] Step {currentStep}: High HDOP / Degraded satellite lock (accuracy {accuracy}m).");
                }

                var transitPacket = CreatePacket(
                    device,
                    lat,
                    lon,
                    currentSpeed,
                    (decimal)bearing,
                    accuracy,
                    ignition: true,
                    simTime,
                    $"{{ \"segment\": \"{currentStop.SequenceOrder}->{nextStop.SequenceOrder}\", \"fraction\": {fraction:F2} }}"
                );

                generatedPackets.Add(transitPacket);
                offlineBatchedPackets += await DispatchPacketAsync(transitPacket, currentStep, options, offlineQueue, logEvents, cancellationToken);
                totalPacketsIngested++;
            }
        }

        // Flush any remaining offline queued packets if still buffered at simulation end
        if (offlineQueue.Count > 0)
        {
            logEvents.Add($"[OFFLINE_FLUSH] Cellular reconnect detected at simulation completion. Syncing {offlineQueue.Count} queued points.");
            var batchRequest = new GpsBatchTelemetryRequest(
                device.DeviceCode,
                device.ApiKey,
                device.Imei,
                offlineQueue.ToList()
            );

            await _ingestionService.IngestBatchAsync(batchRequest, cancellationToken);
            offlineBatchedPackets += offlineQueue.Count;
            offlineQueue.Clear();
        }

        var duration = simTime - startTime;
        logEvents.Add($"[SIM_COMPLETE] Advanced GPS Simulation finished. Ingested {totalPacketsIngested} points ({offlineBatchedPackets} batched offline), duration: {duration:hh\\:mm\\:ss}.");

        return new AdvancedGpsSimulationResult(
            trip.Id,
            trip.BusId,
            trip.Bus.BusNumber,
            trip.Route?.Name ?? "Unknown Route",
            generatedPackets.Count,
            totalPacketsIngested,
            offlineBatchedPackets,
            geofenceArrivalTriggers,
            duration,
            generatedPackets.AsReadOnly(),
            logEvents.AsReadOnly()
        );
    }

    private async Task<int> DispatchPacketAsync(
        GpsTelemetryPacket packet,
        int step,
        AdvancedGpsSimulationOptions options,
        List<GpsTelemetryPointDto> offlineQueue,
        List<string> logEvents,
        CancellationToken cancellationToken)
    {
        int flushedCount = 0;
        bool isOffline = options.SimulateOfflineDisruption &&
                         step >= options.DisruptionStartStep &&
                         step < options.DisruptionStartStep + options.DisruptionDurationSteps;

        if (isOffline)
        {
            offlineQueue.Add(new GpsTelemetryPointDto(
                packet.Latitude,
                packet.Longitude,
                packet.SpeedKmh,
                packet.Heading,
                packet.Altitude,
                packet.AccuracyMeters,
                packet.IgnitionOn,
                packet.RecordedAt
            ));

            logEvents.Add($"[OFFLINE_BUFFER] Step {step}: Cellular link down. Buffered packet at ({packet.Latitude:F5}, {packet.Longitude:F5}) into local memory queue.");
        }
        else
        {
            // If we just recovered from offline, flush the buffer
            if (offlineQueue.Count > 0)
            {
                flushedCount = offlineQueue.Count;
                logEvents.Add($"[CELLULAR_RECONNECT] Step {step}: Cellular connection restored! Flushing {offlineQueue.Count} buffered packets to gateway.");
                var batchRequest = new GpsBatchTelemetryRequest(
                    packet.DeviceCode,
                    packet.ApiKey,
                    packet.Imei,
                    offlineQueue.ToList()
                );

                await _ingestionService.IngestBatchAsync(batchRequest, cancellationToken);
                offlineQueue.Clear();
            }

            await _ingestionService.IngestAsync(packet, cancellationToken);
        }

        return flushedCount;
    }

    private static GpsTelemetryPacket CreatePacket(
        GpsDevice device,
        decimal lat,
        decimal lon,
        decimal speed,
        decimal heading,
        decimal accuracy,
        bool ignition,
        DateTimeOffset recordedAt,
        string payload)
    {
        return new GpsTelemetryPacket(
            device.DeviceCode,
            device.ApiKey,
            device.Imei,
            lat,
            lon,
            speed,
            heading,
            Altitude: 45m,
            AccuracyMeters: accuracy,
            IgnitionOn: ignition,
            RecordedAt: recordedAt,
            RawPayload: payload
        );
    }

    private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double y = Math.Sin(dLon) * Math.Cos(lat2 * Math.PI / 180.0);
        double x = Math.Cos(lat1 * Math.PI / 180.0) * Math.Sin(lat2 * Math.PI / 180.0) -
                   Math.Sin(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Cos(dLon);

        double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (brng + 360.0) % 360.0;
    }
}
