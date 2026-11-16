using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Immutable audit log capturing state transitions across the trip lifecycle.
/// </summary>
public sealed class TripStatusHistory : TenantEntity
{
    public Guid TripId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Trip? Trip { get; set; }
}

