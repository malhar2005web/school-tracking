namespace SchoolTrack.Shared.DTOs.Fleet;

public sealed record BusDto(
    Guid Id,
    Guid SchoolId,
    string BusNumber,
    string RegistrationNumber,
    string? Model,
    int Capacity,
    string MaintenanceStatus,
    DateTimeOffset? InsuranceExpiry,
    DateTimeOffset? FitnessExpiry,
    bool IsActive,
    Guid? CurrentDriverId,
    string? CurrentDriverName,
    string? GpsDeviceCode,
    string? RfidDeviceCode);

public sealed record CreateBusRequest(
    string BusNumber,
    string RegistrationNumber,
    string? Model,
    int Capacity,
    DateTimeOffset? InsuranceExpiry,
    DateTimeOffset? FitnessExpiry);

public sealed record AssignDriverRequest(
    Guid DriverId);

