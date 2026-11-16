using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Constants;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Execution instance of a route for a specific date and morning/afternoon shift.
/// </summary>
public sealed class Trip : TenantEntity
{
    public Guid RouteId { get; set; }
    public Guid BusId { get; set; }
    public Guid DriverId { get; set; }
    public DateOnly TripDate { get; set; }
    public string Shift { get; set; } = TripShifts.Morning;
    public string Status { get; set; } = TripStatuses.Scheduled;
    public DateTimeOffset? ScheduledStartAt { get; set; }
    public DateTimeOffset? ActualStartAt { get; set; }
    public DateTimeOffset? ActualEndAt { get; set; }
    public string? CancellationReason { get; set; }

    public Route? Route { get; set; }
    public Bus? Bus { get; set; }
    public Driver? Driver { get; set; }
    public ICollection<TripStop> Stops { get; set; } = new List<TripStop>();
    public ICollection<TripStatusHistory> StatusHistory { get; set; } = new List<TripStatusHistory>();

    /// <summary>
    /// Executes a validated state transition on the trip state machine, recording history and timestamps.
    /// </summary>
    public TripStatusHistory TransitionTo(
        string targetStatus,
        Guid changedByUserId,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (!TripStatuses.CanTransition(Status, targetStatus))
        {
            throw new InvalidOperationException($"Invalid trip state transition from '{Status}' to '{targetStatus}'.");
        }

        var fromStatus = Status;
        Status = targetStatus;

        if (targetStatus == TripStatuses.Started && !ActualStartAt.HasValue)
        {
            ActualStartAt = utcNow;
        }
        else if (targetStatus is TripStatuses.Completed or TripStatuses.Cancelled && !ActualEndAt.HasValue)
        {
            ActualEndAt = utcNow;
            if (targetStatus == TripStatuses.Cancelled && !string.IsNullOrWhiteSpace(notes))
            {
                CancellationReason = notes;
            }
        }

        var history = new TripStatusHistory
        {
            Id = Guid.CreateVersion7(),
            SchoolId = SchoolId,
            TripId = Id,
            FromStatus = fromStatus,
            ToStatus = targetStatus,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            ChangedAt = utcNow
        };

        StatusHistory.Add(history);
        return history;
    }
}

