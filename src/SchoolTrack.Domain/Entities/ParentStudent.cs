using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Associative relationship linking parents to students with guardian authority flags.
/// </summary>
public sealed class ParentStudent : TenantEntity
{
    public Guid ParentId { get; set; }
    public Guid StudentId { get; set; }
    public string Relationship { get; set; } = "Guardian"; // Mother, Father, Guardian
    public bool IsPrimaryGuardian { get; set; } = true;
    public bool CanAuthorizePickup { get; set; } = true;

    public Parent? Parent { get; set; }
    public Student? Student { get; set; }
}

