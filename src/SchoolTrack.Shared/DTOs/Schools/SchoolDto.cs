namespace SchoolTrack.Shared.DTOs.Schools;

/// <summary>
/// School tenant summary representation.
/// </summary>
public sealed record SchoolDto(
    Guid Id,
    string Name,
    string Code,
    string TimeZoneId,
    string ContactEmail,
    string ContactPhone,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    SchoolSettingsDto? Settings);

