namespace SchoolTrack.Shared.Constants;

/// <summary>
/// Canonical API routes and URL prefixes for the SchoolTrack platform.
/// </summary>
public static class ApiRoutes
{
    public const string Version1Prefix = "api/v1";

    public const string HealthLive = "/health/live";
    public const string HealthReady = "/health/ready";
    public const string Health = "/health";

    public static class Auth
    {
        public const string Base = Version1Prefix + "/auth";
        public const string Login = "login";
        public const string RefreshToken = "refresh-token";
        public const string Logout = "logout";
        public const string Me = "me";
    }

    public static class Schools
    {
        public const string Base = Version1Prefix + "/schools";
        public const string Id = "{id:guid}";
        public const string Settings = "{id:guid}/settings";
    }

    public static class Classes
    {
        public const string Base = Version1Prefix + "/classes";
        public const string Id = "{id:guid}";
    }

    public static class Students
    {
        public const string Base = Version1Prefix + "/students";
        public const string Id = "{id:guid}";
        public const string Parents = "{id:guid}/parents";
    }

    public static class Parents
    {
        public const string Base = Version1Prefix + "/parents";
        public const string Id = "{id:guid}";
        public const string LinkStudent = "{id:guid}/students";
    }

    public static class Drivers
    {
        public const string Base = Version1Prefix + "/drivers";
        public const string Id = "{id:guid}";
    }

    public static class Buses
    {
        public const string Base = Version1Prefix + "/buses";
        public const string Id = "{id:guid}";
        public const string AssignDriver = "{id:guid}/assign-driver";
        public const string Location = "{id:guid}/location";
    }

    public static class Devices
    {
        public const string Base = Version1Prefix + "/devices";
        public const string RfidCards = "rfid-cards";
        public const string RfidCardAssign = "rfid-cards/{id:guid}/assign";
        public const string GpsDevices = "gps-devices";
        public const string RfidDevices = "rfid-devices";
        public const string GpsTelemetry = "gps/telemetry";
        public const string GpsBatchTelemetry = "gps/telemetry/batch";
        public const string GpsSimulate = "gps/simulate";
        public const string RfidTap = "rfid/tap";
        public const string RfidBatchTap = "rfid/tap/batch";
        public const string RfidSimulateTap = "rfid/simulate-tap";
        public const string UnknownCardTaps = "rfid/unknown-taps";
    }

    public static class Routes
    {
        public const string Base = Version1Prefix + "/routes";
        public const string Id = "{id:guid}";
        public const string Stops = "{id:guid}/stops";
        public const string Assignments = "assignments";
    }

    public static class Trips
    {
        public const string Base = Version1Prefix + "/trips";
        public const string Id = "{id:guid}";
        public const string Status = "{id:guid}/status";
        public const string StopStatus = "{id:guid}/stops/{stopId:guid}/status";
        public const string Active = "active";
        public const string Trail = "{id:guid}/trail";
        public const string Manifest = "{id:guid}/manifest";
    }
}
