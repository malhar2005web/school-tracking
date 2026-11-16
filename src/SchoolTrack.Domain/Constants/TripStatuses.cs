namespace SchoolTrack.Domain.Constants;

/// <summary>
/// State machine statuses for daily student transportation trips.
/// </summary>
public static class TripStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Started = "Started";
    public const string InTransit = "InTransit";
    public const string Arrived = "Arrived";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Scheduled, Started, InTransit, Arrived, Completed, Cancelled];

    /// <summary>
    /// Evaluates whether a state transition is permitted by the SchoolTrack trip state machine.
    /// </summary>
    public static bool CanTransition(string currentStatus, string targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (Scheduled, Started) => true,
            (Scheduled, Cancelled) => true,
            (Started, InTransit) => true,
            (Started, Cancelled) => true,
            (InTransit, Arrived) => true,
            (InTransit, Cancelled) => true,
            (Arrived, Completed) => true,
            (Arrived, Cancelled) => true,
            _ => false
        };
    }
}

