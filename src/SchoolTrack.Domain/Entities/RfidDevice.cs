using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Physical Ethernet/cellular RFID reader mounted near a bus door logging student taps.
/// </summary>
public sealed class RfidDevice : TenantEntity
{
    public string DeviceCode { get; set; } = string.Empty;
    public Guid? BusId { get; set; }
    public string? ApiKey { get; set; }
    public string? MacAddress { get; set; }
    public string? GateLocation { get; set; }
    public string? Model { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string ProtocolType { get; set; } = "Simulator"; // Http, Tcp, Udp, Vendor, Simulator
    public string Status { get; set; } = "Offline"; // Online, Offline, Provisioning
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LastScanAt { get; set; }

    public Bus? Bus { get; set; }
}

