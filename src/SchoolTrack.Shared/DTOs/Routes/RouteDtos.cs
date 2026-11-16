namespace SchoolTrack.Shared.DTOs.Routes;

public sealed record RouteDto(
    Guid Id,
    Guid SchoolId,
    string Name,
    string Code,
    string DirectionType,
    bool IsActive,
    IReadOnlyList<RouteStopDto> Stops);

public sealed record RouteStopDto(
    Guid Id,
    Guid RouteId,
    string StopName,
    int SequenceOrder,
    decimal Latitude,
    decimal Longitude,
    int ScheduledOffsetMinutes,
    int GeofenceRadiusMeters);

public sealed record CreateRouteRequest(
    string Name,
    string Code,
    string DirectionType);

public sealed record AddRouteStopRequest(
    string StopName,
    int SequenceOrder,
    decimal Latitude,
    decimal Longitude,
    int ScheduledOffsetMinutes,
    int GeofenceRadiusMeters = 50);

public sealed record RouteAssignmentDto(
    Guid Id,
    Guid SchoolId,
    Guid RouteId,
    string RouteName,
    Guid BusId,
    string BusNumber,
    Guid DriverId,
    string DriverName,
    string Shift,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive);

public sealed record CreateRouteAssignmentRequest(
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string Shift,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo = null);

