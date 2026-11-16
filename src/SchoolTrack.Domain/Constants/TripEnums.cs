namespace SchoolTrack.Domain.Constants;

public static class RouteDirectionTypes
{
    public const string MorningPickup = "MorningPickup";
    public const string AfternoonDrop = "AfternoonDrop";
    public const string Both = "Both";

    public static readonly string[] All = [MorningPickup, AfternoonDrop, Both];
}

public static class TripShifts
{
    public const string Morning = "Morning";
    public const string Afternoon = "Afternoon";

    public static readonly string[] All = [Morning, Afternoon];
}

public static class TripStopStatuses
{
    public const string Pending = "Pending";
    public const string Arrived = "Arrived";
    public const string Departed = "Departed";
    public const string Skipped = "Skipped";

    public static readonly string[] All = [Pending, Arrived, Departed, Skipped];
}

