namespace SchoolTrack.Shared.DTOs.Schools;

/// <summary>
/// Operational settings governing hardware and scan thresholds for a school tenant.
/// </summary>
public sealed record SchoolSettingsDto(
    Guid Id,
    Guid SchoolId,
    int DuplicateScanWindowSeconds,
    int DeviceOfflineThresholdSeconds,
    int GpsRetentionDays,
    bool EnableSmsFallback,
    DateTimeOffset? UpdatedAt);

