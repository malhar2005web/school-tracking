using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Parent or legal guardian profile linked to an authenticated user account and student records.
/// </summary>
public sealed class Parent : TenantEntity
{
    public Guid UserId { get; set; }
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Address { get; set; }

    public User? User { get; set; }
    public ICollection<ParentStudent> StudentLinks { get; set; } = new List<ParentStudent>();
}

