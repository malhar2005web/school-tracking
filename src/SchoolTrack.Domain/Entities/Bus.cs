using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// School bus vehicle equipped with GPS tracking and RFID boarding readers.
/// </summary>
public sealed class Bus : TenantEntity
{
    public string BusNumber { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int Capacity { get; set; } = 30;
    public string MaintenanceStatus { get; set; } = "Active"; // Active, Maintenance, Retired
    public DateTimeOffset? InsuranceExpiry { get; set; }
    public DateTimeOffset? FitnessExpiry { get; set; }
    public bool IsActive { get; set; } = true;

    public GpsDevice? GpsDevice { get; set; }
    public RfidDevice? RfidDevice { get; set; }
    public ICollection<BusAssignment> Assignments { get; set; } = new List<BusAssignment>();
}

