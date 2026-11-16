namespace SchoolTrack.ParentApp.Models;

public sealed record ChildProfileModel(
    Guid StudentId,
    string FullName,
    string Grade,
    string ClassSection,
    string AdmissionNumber,
    string SchoolName,
    string AvatarUrl,
    string RfidCardUid,
    bool IsRfidCardActive,
    Guid BusId,
    string BusNumber,
    string RouteName,
    string PrimaryDriverName,
    string DriverPhoneNumber,
    string DriverAvatarUrl,
    double DriverRating,
    string MorningPickupStopName,
    string MorningPickupTime,
    string AfternoonDropOffStopName,
    string AfternoonDropOffTime,
    bool IsOnBus);

public sealed record TripWaypointModel(
    int SequenceOrder,
    string StopName,
    decimal Latitude,
    decimal Longitude,
    string ScheduledTime,
    bool IsCompleted,
    bool IsCurrentNext,
    bool IsChildStop);

public sealed record ChildActiveTripModel(
    Guid TripId,
    Guid SchoolId,
    Guid BusId,
    string BusNumber,
    string RouteName,
    string DriverName,
    string DriverPhone,
    string DriverAvatarUrl,
    double DriverRating,
    decimal CurrentLatitude,
    decimal CurrentLongitude,
    decimal SpeedKmh,
    decimal HeadingDegrees,
    string CurrentStreetLocation,
    string ApproachingStopName,
    string EstimatedArrivalTime,
    int MinutesRemaining,
    int CurrentStopIndex,
    int TotalStops,
    int CompletionPercentage,
    bool IsLive,
    string WeatherStatus,
    string LastRfidBoardingInfo,
    DateTimeOffset LastUpdated,
    IReadOnlyList<TripWaypointModel> Waypoints);

public sealed record RfidTransitLogModel(
    Guid LogId,
    DateTimeOffset Timestamp,
    string FormattedTime,
    string MilestoneTitle,
    string LocationName,
    string ScanType, // Boarding, EnRouteCheckpoint, SchoolGate, DropOff
    string BusNumber,
    string DriverName,
    bool IsRfidVerified,
    string? MapSnapshotUrl,
    string StatusNote);

public sealed record ParentNotificationModel(
    Guid Id,
    string Title,
    string Message,
    DateTimeOffset Timestamp,
    string Type, // Boarding, Delay, RouteAlert, SOS, General
    bool IsRead,
    string ChildName);

public sealed record AbsenceReportRequest(
    Guid StudentId,
    DateTime Date,
    string Reason, // Sick, Doctor, Family, Vacation, Other
    string Details,
    bool MorningTrip,
    bool AfternoonTrip);

public sealed class ParentSafetySettings
{
    public bool PushAlertsOnBoarding { get; set; } = true;
    public bool SmsFallbackEnabled { get; set; } = true;
    public bool SoundOnBoardingChime { get; set; } = true;
    public bool DelayAlertsEnabled { get; set; } = true;
    public bool SafeArrivalConfirmations { get; set; } = true;
    public bool GeofenceRadiusAlerts { get; set; } = true;
}

public sealed record AuthorizedGuardianModel(
    Guid Id,
    string FullName,
    string Relationship,
    string PhoneNumber,
    string AvatarUrl,
    bool IsPrimary,
    bool CanAuthorizePickup);

public sealed record ParentSession(
    Guid ParentUserId,
    Guid SchoolId,
    string FullName,
    string Email,
    string PhoneNumber,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<ChildProfileModel> LinkedChildren);

