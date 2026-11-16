using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Operational hardware and scan configuration settings for an individual school tenant.
/// </summary>
public sealed class SchoolSettings : BaseEntity
{
    public Guid SchoolId { get; set; }

    /// <summary>
    /// Deduplication window (in seconds) to suppress rapid successive taps of the same RFID card.
    /// </summary>
    public int DuplicateScanWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Inactivity threshold (in seconds) after which a GPS or RFID hardware unit is flagged offline.
    /// </summary>
    public int DeviceOfflineThresholdSeconds { get; set; } = 180;

    /// <summary>
    /// Retention window (in days) for raw GPS location breadcrumbs.
    /// </summary>
    public int GpsRetentionDays { get; set; } = 90;

    /// <summary>
    /// Whether SMS fallback is triggered when mobile push notifications fail.
    /// </summary>
    public bool EnableSmsFallback { get; set; }

    public School? School { get; set; }
}

