namespace SchoolTrack.Shared.DTOs.Devices;

public sealed record RfidCardDto(
    Guid Id,
    Guid SchoolId,
    string CardUid,
    Guid? StudentId,
    string? StudentName,
    string CardType,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt);

public sealed record CreateRfidCardRequest(
    string CardUid,
    string CardType,
    Guid? StudentId);

public sealed record AssignRfidCardRequest(
    Guid StudentId);

public sealed record GpsDeviceDto(
    Guid Id,
    Guid SchoolId,
    string DeviceCode,
    Guid? BusId,
    string? BusNumber,
    string? Imei,
    string? Model,
    string ProtocolType,
    string Status,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? LastLocationAt);

public sealed record RegisterGpsDeviceRequest(
    string DeviceCode,
    Guid? BusId,
    string? Imei,
    string? Model,
    string? SimCardNumber,
    string? ProtocolType);

public sealed record RfidDeviceDto(
    Guid Id,
    Guid SchoolId,
    string DeviceCode,
    Guid? BusId,
    string? BusNumber,
    string? GateLocation,
    string? Model,
    string? IpAddress,
    int? Port,
    string ProtocolType,
    string Status,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? LastScanAt);

public sealed record RegisterRfidDeviceRequest(
    string DeviceCode,
    Guid? BusId,
    string? GateLocation,
    string? Model,
    string? IpAddress,
    int? Port,
    string? ProtocolType);

