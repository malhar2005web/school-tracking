namespace SchoolTrack.DriverApp.Models;

public sealed record DriverSession(
    Guid DriverId,
    Guid SchoolId,
    string FullName,
    string LicenseNumber,
    string PhoneNumber,
    string AvatarUrl,
    Guid BusId,
    string BusNumber,
    Guid RouteId,
    string RouteName,
    string AccessToken,
    string ShiftStatus); // Scheduled, PreTripPending, InTransit, Completed

public sealed class DriverManifestStudentModel
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string Grade { get; set; } = "";
    public string ClassSection { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string RfidCardUid { get; set; } = "";
    public Guid StopId { get; set; }
    public string StopName { get; set; } = "";
    public int StopSequence { get; set; }
    public bool IsBoarded { get; set; }
    public DateTimeOffset? BoardedAt { get; set; }
    public string? BoardingMethod { get; set; } // "RfidScan", "ManualOverride"
    public bool IsDeboarded { get; set; }
    public DateTimeOffset? DeboardedAt { get; set; }
    public string? DeboardingMethod { get; set; }
    public bool IsAbsent { get; set; }
    public string? AbsenceReason { get; set; } // "ParentExcused", "Doctor Appt", "DriverMarked"
    public string AvatarUrl { get; set; } = "";
    public string ParentName { get; set; } = "";
    public string ParentPhone { get; set; } = "";

    public string InOutStatus
    {
        get
        {
            if (IsAbsent) return "ABSENT";
            if (IsDeboarded) return "OUT (Deboarded)";
            if (IsBoarded) return "IN (On-Bus)";
            return "PENDING";
        }
    }
}

public sealed record DriverActiveStopModel(
    Guid StopId,
    string StopName,
    int SequenceOrder,
    string ScheduledTime,
    string EstimatedTime,
    int TotalStudentsAtStop,
    int BoardedCount,
    bool IsActiveCurrent,
    bool IsCompleted,
    decimal Latitude,
    decimal Longitude);

public sealed record DriverActiveRouteModel(
    Guid TripId,
    Guid RouteId,
    string RouteName,
    Guid BusId,
    string BusNumber,
    decimal SpeedKmh,
    int SpeedLimitKmh,
    string NextTurnInstruction,
    string DistanceToNextTurn,
    string NextStopName,
    decimal GpsAccuracyMeters,
    bool RfidHardwareOnline,
    int OnboardCount,
    int ToPickUpCount,
    int SchoolTargetCount,
    int CurrentStopIndex,
    int TotalStops,
    IReadOnlyList<DriverActiveStopModel> Stops,
    string Shift = "Morning", // "Morning" (Pickup) or "Evening" (Drop)
    bool IsTripClosed = false,
    DateTimeOffset? ClosedAt = null,
    int DeliveredCount = 0);

public sealed class DriverPreTripChecklist
{
    public bool BrakesAndTiresPassed { get; set; } = true;
    public bool EmergencyExitsVerified { get; set; } = true;
    public bool FirstAidKitStocked { get; set; } = true;
    public bool RfidReaderTested { get; set; } = true;
    public bool GpsTrackerTested { get; set; } = true;
    public bool InteriorCleanSweepCompleted { get; set; } = true;
    public string Notes { get; set; } = "All vehicle safety systems operational.";
    public DateTimeOffset InspectedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record BroadcastDelayRequest(
    int MinutesDelay,
    string Reason, // "Heavy Traffic", "Construction", "Weather", "Mechanical Issue", "Accident Ahead"
    string CustomNotes,
    bool BroadcastToParents,
    bool BroadcastToDispatch);

public sealed record DriverSosAlertRequest(
    Guid BusId,
    string BusNumber,
    Guid TripId,
    decimal Latitude,
    decimal Longitude,
    string Reason);

