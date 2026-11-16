namespace SchoolTrack.Shared.DTOs.Trips;

public sealed record TripDto(
    Guid Id,
    Guid SchoolId,
    Guid RouteId,
    string RouteName,
    Guid BusId,
    string BusNumber,
    Guid DriverId,
    string DriverName,
    DateOnly TripDate,
    string Shift,
    string Status,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ActualStartAt,
    DateTimeOffset? ActualEndAt,
    string? CancellationReason,
    IReadOnlyList<TripStopDto> Stops);

public sealed record TripStopDto(
    Guid Id,
    Guid TripId,
    Guid RouteStopId,
    string StopName,
    int SequenceOrder,
    decimal Latitude,
    decimal Longitude,
    string Status,
    DateTimeOffset? EstimatedArrivalAt,
    DateTimeOffset? ActualArrivalAt,
    DateTimeOffset? ActualDepartureAt);

public sealed record TripStatusHistoryDto(
    Guid Id,
    Guid TripId,
    string FromStatus,
    string ToStatus,
    Guid ChangedByUserId,
    string? Notes,
    DateTimeOffset ChangedAt);

public sealed record CreateTripRequest(
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    DateOnly TripDate,
    string Shift,
    DateTimeOffset? ScheduledStartAt = null);

public sealed record UpdateTripStatusRequest(
    string TargetStatus,
    string? Notes = null);

public sealed record UpdateTripStopStatusRequest(
    string Status);

