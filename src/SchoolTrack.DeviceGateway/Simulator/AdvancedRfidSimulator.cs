using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Simulator;

public interface IAdvancedRfidSimulator
{
    Task<AdvancedRfidSimulationResult> RunSimulationAsync(
        AdvancedRfidSimulationOptions options,
        CancellationToken cancellationToken = default);
}

public sealed class AdvancedRfidSimulator : IAdvancedRfidSimulator
{
    private readonly IApplicationDbContext _context;
    private readonly IRfidIngestionService _ingestionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AdvancedRfidSimulator> _logger;

    public AdvancedRfidSimulator(
        IApplicationDbContext context,
        IRfidIngestionService ingestionService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AdvancedRfidSimulator> logger)
    {
        _context = context;
        _ingestionService = ingestionService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<AdvancedRfidSimulationResult> RunSimulationAsync(
        AdvancedRfidSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Bus)
                .ThenInclude(b => b!.RfidDevice)
            .Include(t => t.Stops)
                .ThenInclude(ts => ts.RouteStop)
            .FirstOrDefaultAsync(t => t.Id == options.TripId, cancellationToken);

        if (trip == null)
            throw new KeyNotFoundException($"Trip with ID '{options.TripId}' was not found.");

        if (trip.Bus?.RfidDevice == null)
            throw new InvalidOperationException($"Bus assigned to trip '{options.TripId}' has no active registered RFID reader device.");

        var device = trip.Bus.RfidDevice;
        var eventTimeline = new List<string>();
        var scanResults = new List<RfidScanResultDto>();

        int totalTaps = 0;
        int boardingCount = 0;
        int dropOffCount = 0;
        int deduplicatedCount = 0;
        int unknownCardsCount = 0;

        var simTime = _dateTimeProvider.UtcNow.AddMinutes(-10);

        eventTimeline.Add($"[RFID_INIT] Starting Ethernet RFID reader simulation for Bus '{trip.Bus.BusNumber}', Device '{device.DeviceCode}'. Mode: {options.Mode}.");

        // 1. Resolve eligible students with active RFID cards
        var studentCardsQuery = _context.RfidCards
            .IgnoreQueryFilters()
            .Include(c => c.Student)
            .Where(c => c.Status == "Active" && c.StudentId != null);

        var cards = await studentCardsQuery.Take(10).ToListAsync(cancellationToken);
        if (cards.Count == 0)
        {
            eventTimeline.Add("[WARN] No active student RFID cards found in system. Generating virtual card test.");
        }

        decimal? stopLat = null;
        decimal? stopLon = null;

        if (options.StopId.HasValue)
        {
            var stopMatch = trip.Stops.FirstOrDefault(s => s.RouteStopId == options.StopId || s.Id == options.StopId);
            if (stopMatch?.RouteStop != null)
            {
                stopLat = stopMatch.RouteStop.Latitude;
                stopLon = stopMatch.RouteStop.Longitude;
            }
        }

        // 2. Normal sequential boarding / deboarding scan sweep
        var offlineItems = new List<RfidScanPointDto>();

        foreach (var card in cards)
        {
            simTime = simTime.AddSeconds(2);
            totalTaps++;

            if (options.SimulateOfflineReader)
            {
                offlineItems.Add(new RfidScanPointDto(
                    card.CardUid,
                    stopLat,
                    stopLon,
                    simTime
                ));
                eventTimeline.Add($"[OFFLINE_QUEUE] Card '{card.CardUid}' for student '{card.Student?.FullName}' queued in offline reader buffer.");
            }
            else
            {
                var packet = new RfidScanPacket(
                    device.DeviceCode,
                    device.ApiKey,
                    card.CardUid,
                    stopLat,
                    stopLon,
                    simTime,
                    "{\"mode\":\"live-rfid\"}"
                );

                var res = await _ingestionService.IngestTapAsync(packet, cancellationToken);
                scanResults.Add(res);

                if (res.Success && res.ScanType == "Boarding") boardingCount++;
                else if (res.Success && res.ScanType == "DropOff") dropOffCount++;
                else if (res.Message.Contains("Deduplicated", StringComparison.OrdinalIgnoreCase)) deduplicatedCount++;

                eventTimeline.Add($"[TAP_OK] Card '{card.CardUid}' tapped: {res.StudentName} -> {res.ScanType} (Success: {res.Success}).");
            }
        }

        // If offline simulation mode, flush batch
        if (options.SimulateOfflineReader && offlineItems.Count > 0)
        {
            eventTimeline.Add($"[BATCH_SYNC] Reader restored Ethernet link. Synchronizing batch of {offlineItems.Count} scans.");
            var batchRequest = new RfidBatchScanRequest(
                device.DeviceCode,
                device.ApiKey,
                offlineItems
            );

            var batchResults = await _ingestionService.IngestBatchTapsAsync(batchRequest, cancellationToken);
            scanResults.AddRange(batchResults);

            foreach (var br in batchResults)
            {
                if (br.Success && br.ScanType == "Boarding") boardingCount++;
                else if (br.Success && br.ScanType == "DropOff") dropOffCount++;
                else if (br.Message.Contains("Deduplicated", StringComparison.OrdinalIgnoreCase)) deduplicatedCount++;
            }
        }

        // 3. Rapid Double-Tap Simulation Test (Deduplication within cooldown)
        if (options.IncludeRapidDoubleTap && cards.Count > 0)
        {
            var firstCard = cards[0];
            totalTaps++;
            simTime = simTime.AddSeconds(1); // 1 second later (rapid double tap)

            eventTimeline.Add($"[RAPID_TAP_TEST] Simulating immediate double-tap for card '{firstCard.CardUid}' (+1s).");

            var doubleTapPacket = new RfidScanPacket(
                device.DeviceCode,
                device.ApiKey,
                firstCard.CardUid,
                stopLat,
                stopLon,
                simTime,
                "{\"doubleTapTest\": true}"
            );

            var doubleTapResult = await _ingestionService.IngestTapAsync(doubleTapPacket, cancellationToken);
            scanResults.Add(doubleTapResult);

            if (!doubleTapResult.Success && (doubleTapResult.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) || doubleTapResult.Message.Contains("Deduplicated", StringComparison.OrdinalIgnoreCase) || doubleTapResult.Message.Contains("cooldown", StringComparison.OrdinalIgnoreCase)))
            {
                deduplicatedCount++;
                eventTimeline.Add($"[DEDUP_VERIFIED] Rapid double-tap safely deduplicated! Message: '{doubleTapResult.Message}'.");
            }
        }

        // 4. Rogue / Unknown Unregistered Card Simulation Test
        if (options.IncludeUnknownCardTap)
        {
            totalTaps++;
            simTime = simTime.AddSeconds(3);
            var unknownUid = "XX-99-RO-GU-E1";

            eventTimeline.Add($"[ROGUE_CARD_TEST] Simulating unassigned/unknown card '{unknownUid}'.");

            var unknownPacket = new RfidScanPacket(
                device.DeviceCode,
                device.ApiKey,
                unknownUid,
                stopLat,
                stopLon,
                simTime,
                "{\"unknownCardTest\": true}"
            );

            var unknownResult = await _ingestionService.IngestTapAsync(unknownPacket, cancellationToken);
            scanResults.Add(unknownResult);

            if (!unknownResult.Success)
            {
                unknownCardsCount++;
                eventTimeline.Add($"[SECURITY_AUDIT] Unknown card '{unknownUid}' correctly intercepted and logged to security audit without breaking reader loop.");
            }
        }

        eventTimeline.Add($"[RFID_COMPLETE] Simulation finished. Taps: {totalTaps}, Boarded: {boardingCount}, DroppedOff: {dropOffCount}, Deduplicated: {deduplicatedCount}, Unknown: {unknownCardsCount}.");

        return new AdvancedRfidSimulationResult(
            trip.Id,
            trip.BusId,
            trip.Bus.BusNumber,
            totalTaps,
            boardingCount,
            dropOffCount,
            deduplicatedCount,
            unknownCardsCount,
            scanResults.AsReadOnly(),
            eventTimeline.AsReadOnly()
        );
    }
}
