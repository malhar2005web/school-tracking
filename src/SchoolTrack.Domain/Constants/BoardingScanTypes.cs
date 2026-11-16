namespace SchoolTrack.Domain.Constants;

/// <summary>
/// Canonical scan event types for student bus ingress and egress.
/// </summary>
public static class BoardingScanTypes
{
    public const string Boarding = "Boarding";
    public const string DropOff = "DropOff";
    public const string BoardingCancelled = "BoardingCancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Boarding,
        DropOff,
        BoardingCancelled
    };
}

