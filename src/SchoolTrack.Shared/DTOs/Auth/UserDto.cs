namespace SchoolTrack.Shared.DTOs.Auth;

/// <summary>
/// Authenticated user representation returned with authentication payloads.
/// </summary>
public sealed record UserDto(
    Guid Id,
    Guid? SchoolId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string? SchoolName);

