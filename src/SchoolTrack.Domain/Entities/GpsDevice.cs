using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Dedicated telematics GPS tracker mounted on a bus transmitting live coordinates.
/// </summary>
public sealed class GpsDevice : TenantEntity
{
    public string DeviceCode { get; set; } = string.Empty;
    public Guid? BusId { get; set; }
    public string? Imei { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? SimCardNumber { get; set; }
    public string ProtocolType { get; set; } = "Simulator"; // Http, Tcp, Udp, Mqtt, Vendor, Simulator
    public string Status { get; set; } = "Offline"; // Online, Offline, Provisioning
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LastLocationAt { get; set; }

    public Bus? Bus { get; set; }
}

