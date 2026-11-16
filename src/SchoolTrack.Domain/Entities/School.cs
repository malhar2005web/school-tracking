using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Represents an individual school tenant operating transportation services on SchoolTrack.
/// </summary>
public sealed class School : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public SchoolSettings? Settings { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
