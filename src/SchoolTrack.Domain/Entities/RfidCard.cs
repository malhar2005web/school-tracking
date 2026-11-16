using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Physical passive RFID smart card assigned to a student for tap-in/tap-out boarding events.
/// </summary>
public sealed class RfidCard : TenantEntity
{
    public string CardUid { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public string CardType { get; set; } = "Permanent"; // Permanent, Temporary
    public string Status { get; set; } = "Active"; // Active, Lost, Revoked, Unassigned
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }

    public Student? Student { get; set; }
}

