namespace SchoolTrack.Shared.DTOs.Schools;

/// <summary>
/// Request payload to register a new school tenant and its initial administrative account.
/// </summary>
public sealed record CreateSchoolRequest(
    string Name,
    string Code,
    string? TimeZoneId,
    string ContactEmail,
    string ContactPhone,
    string? Address,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName);

