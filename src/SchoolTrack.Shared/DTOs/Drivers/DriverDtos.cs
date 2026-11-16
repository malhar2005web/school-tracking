namespace SchoolTrack.Shared.DTOs.Drivers;

public sealed record DriverDto(
    Guid Id,
    Guid SchoolId,
    Guid UserId,
    string FullName,
    string Email,
    string LicenseNumber,
    DateTimeOffset? LicenseExpiry,
    string Phone,
    string AvailabilityStatus,
    int YearsExperience,
    double Rating);

public sealed record CreateDriverRequest(
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    DateTimeOffset? LicenseExpiry,
    string Phone,
    int YearsExperience);

