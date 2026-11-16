using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Student enrolled in a school tenant who is transported and monitored via RFID scans.
/// </summary>
public sealed class Student : TenantEntity
{
    public Guid ClassId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhotoUrl { get; set; }
    public string? Gender { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? SeatNumber { get; set; }
    public Guid? AssignedRouteId { get; set; }
    public Guid? AssignedStopId { get; set; }
    public bool IsActive { get; set; } = true;

    public Class? Class { get; set; }
    public RfidCard? AssignedRfidCard { get; set; }
    public ICollection<ParentStudent> ParentLinks { get; set; } = new List<ParentStudent>();
}

