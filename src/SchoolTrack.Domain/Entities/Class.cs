using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Academic class/division grouping for students within a school tenant.
/// </summary>
public sealed class Class : TenantEntity
{
    public string Grade { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Name => $"{Grade} - {Division}".Trim();

    public ICollection<Student> Students { get; set; } = new List<Student>();
}

