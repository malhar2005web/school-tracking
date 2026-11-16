using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Simulator;

public sealed record AdvancedGpsSimulationOptions(
    Guid TripId,
    decimal CruiseSpeedKmh = 35m,
    int StepSeconds = 4,
    int StopDwellSeconds = 20,
    bool SimulateOfflineDisruption = false,
    int DisruptionStartStep = 5,
    int DisruptionDurationSteps = 6,
    bool SimulateDegradedGps = false,
    int DegradedStartStep = 3,
    int DegradedDurationSteps = 4);

public sealed record AdvancedGpsSimulationResult(
    Guid TripId,
    Guid BusId,
    string BusNumber,
    string RouteName,
    int TotalWaypointsGenerated,
    int TotalPacketsIngested,
    int OfflineBatchedPackets,
    int GeofenceArrivalTriggers,
    TimeSpan SimulatedDuration,
    IReadOnlyList<GpsTelemetryPacket> GeneratedPackets,
    IReadOnlyList<string> LogEvents);

public sealed record AdvancedRfidSimulationOptions(
    Guid TripId,
    Guid? StopId = null,
    string Mode = "Boarding", // "Boarding", "Deboarding"
    bool IncludeRapidDoubleTap = true,
    bool IncludeUnknownCardTap = true,
    bool IncludeUnassignedCardTap = false,
    bool SimulateOfflineReader = false);

public sealed record AdvancedRfidSimulationResult(
    Guid TripId,
    Guid BusId,
    string BusNumber,
    int TotalTapsAttempted,
    int BoardingEventsCount,
    int DropOffEventsCount,
    int DeduplicatedCount,
    int UnknownCardsFlagged,
    IReadOnlyList<RfidScanResultDto> ScanResults,
    IReadOnlyList<string> EventTimeline);

