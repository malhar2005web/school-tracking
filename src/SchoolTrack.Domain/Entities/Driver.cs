using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Professional school bus driver linked to an authenticated user account.
/// </summary>
public sealed class Driver : TenantEntity
{
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTimeOffset? LicenseExpiry { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = "Available"; // Available, OnTrip, OffDuty
    public int YearsExperience { get; set; }
    public double Rating { get; set; } = 5.0;

    public User? User { get; set; }
    public ICollection<BusAssignment> BusAssignments { get; set; } = new List<BusAssignment>();
}

