namespace SchoolTrack.Admin.Models;

public class AuthSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(8);
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "Elena Vasquez";
    public string Email { get; set; } = "elena.vasquez@oakridge.edu";
    public string Role { get; set; } = "SchoolAdmin";
    public Guid SchoolId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string SchoolName { get; set; } = "Oakridge Academy District #4";
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
}

public class DashboardSummaryViewModel
{
    public int ActiveFleetCount { get; set; } = 18;
    public int TotalFleetCount { get; set; } = 20;
    public int StudentsBoardedCount { get; set; } = 412;
    public int TotalStudentsCount { get; set; } = 438;
    public int GatewaysOnlineCount { get; set; } = 24;
    public int TotalGatewaysCount { get; set; } = 24;
    public int ActiveAlertsCount { get; set; } = 1;
    public string ActiveAlertSummary { get; set; } = "Route #08 Maple Ave Congestion (+7 min)";
    public List<LiveRfidTapItem> RecentTaps { get; set; } = new();
    public List<BusLiveStatusItem> ActiveBuses { get; set; } = new();
}

public class LiveRfidTapItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentName { get; set; } = string.Empty;
    public string CardSerialNumber { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string StopName { get; set; } = string.Empty;
    public string EventType { get; set; } = "Boarded"; // Boarded, Exited, Unknown
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsVerified { get; set; } = true;
    public bool ParentNotified { get; set; } = true;
}

public class BusLiveStatusItem
{
    public Guid BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedMph { get; set; }
    public string Status { get; set; } = "OnSchedule"; // OnSchedule, Delayed, AtStop, Offline
    public string RouteName { get; set; } = string.Empty;
    public string NextStopName { get; set; } = string.Empty;
    public int NextStopEtaMinutes { get; set; }
    public int PassengerCount { get; set; }
    public int Capacity { get; set; } = 28;
    public bool IsSelected { get; set; }
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
}

