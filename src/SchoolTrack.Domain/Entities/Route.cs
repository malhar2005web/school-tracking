using SchoolTrack.Domain.Common;
using SchoolTrack.Domain.Constants;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Predefined transportation path with sequenced stops for morning pickups or afternoon drop-offs.
/// </summary>
public sealed class Route : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DirectionType { get; set; } = RouteDirectionTypes.Both;
    public bool IsActive { get; set; } = true;

    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<RouteAssignment> Assignments { get; set; } = new List<RouteAssignment>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

