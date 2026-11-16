using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Hubs;

/// <summary>
/// Authenticated SignalR hub dispatching real-time bus locations, boarding alerts, and trip lifecycle events.
/// </summary>
[Authorize]
public sealed class TrackingHub : Hub<ITrackingClient>
{
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(ILogger<TrackingHub> logger)
    {
        _logger = logger;
    }

    public static string SchoolGroup(Guid schoolId) => $"school_{schoolId:N}";
    public static string TripGroup(Guid tripId) => $"trip_{tripId:N}";
    public static string BusGroup(Guid busId) => $"bus_{busId:N}";
    public static string ParentGroup(Guid parentUserId) => $"parent_{parentUserId:N}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        var schoolId = GetCurrentSchoolId();

        _logger.LogInformation("SignalR client {ConnectionId} connected (User: {UserId}, Role: {Role}, School: {SchoolId})",
            Context.ConnectionId, userId, userRole, schoolId);

        // Auto-join parent's private notification group
        if (userId.HasValue && userRole == SystemRoles.Parent)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ParentGroup(userId.Value));
            _logger.LogInformation("Auto-subscribed parent {UserId} to private group {Group}", userId, ParentGroup(userId.Value));
        }

        // Auto-join school group for admin/transport staff
        if (schoolId.HasValue && (userRole is SystemRoles.SchoolAdmin or SystemRoles.TransportManager))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SchoolGroup(schoolId.Value));
            _logger.LogInformation("Auto-subscribed staff {UserId} to school group {Group}", userId, SchoolGroup(schoolId.Value));
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client {ConnectionId} disconnected", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSchoolGroup(Guid schoolId)
    {
        EnsureSchoolAccess(schoolId);
        await Groups.AddToGroupAsync(Context.ConnectionId, SchoolGroup(schoolId));
        _logger.LogInformation("Client {ConnectionId} joined school group {SchoolId}", Context.ConnectionId, schoolId);
    }

    public async Task LeaveSchoolGroup(Guid schoolId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SchoolGroup(schoolId));
        _logger.LogInformation("Client {ConnectionId} left school group {SchoolId}", Context.ConnectionId, schoolId);
    }

    public async Task JoinTripGroup(Guid tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TripGroup(tripId));
        _logger.LogInformation("Client {ConnectionId} joined trip group {TripId}", Context.ConnectionId, tripId);
    }

    public async Task LeaveTripGroup(Guid tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TripGroup(tripId));
        _logger.LogInformation("Client {ConnectionId} left trip group {TripId}", Context.ConnectionId, tripId);
    }

    public async Task JoinBusGroup(Guid busId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BusGroup(busId));
        _logger.LogInformation("Client {ConnectionId} joined bus group {BusId}", Context.ConnectionId, busId);
    }

    public async Task LeaveBusGroup(Guid busId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BusGroup(busId));
        _logger.LogInformation("Client {ConnectionId} left bus group {BusId}", Context.ConnectionId, busId);
    }

    public async Task JoinParentGroup(Guid parentUserId)
    {
        var currentUserId = GetCurrentUserId();
        var currentRole = GetCurrentUserRole();

        // Only the parent themselves or platform/school staff may join a parent group
        if (currentRole is not (SystemRoles.SuperAdmin or SystemRoles.SchoolAdmin or SystemRoles.TransportManager))
        {
            if (!currentUserId.HasValue || currentUserId.Value != parentUserId)
            {
                throw new HubException("Unauthorized to subscribe to another parent's notification channel.");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ParentGroup(parentUserId));
        _logger.LogInformation("Client {ConnectionId} joined parent group {ParentId}", Context.ConnectionId, parentUserId);
    }

    public async Task LeaveParentGroup(Guid parentUserId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ParentGroup(parentUserId));
        _logger.LogInformation("Client {ConnectionId} left parent group {ParentId}", Context.ConnectionId, parentUserId);
    }

    private void EnsureSchoolAccess(Guid targetSchoolId)
    {
        var userRole = GetCurrentUserRole();
        if (userRole == SystemRoles.SuperAdmin)
        {
            return;
        }

        var userSchoolId = GetCurrentSchoolId();
        if (!userSchoolId.HasValue || userSchoolId.Value != targetSchoolId)
        {
            throw new HubException("Cross-tenant school group subscriptions are strictly forbidden.");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetCurrentUserRole()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Role);
    }

    private Guid? GetCurrentSchoolId()
    {
        var schoolClaim = Context.User?.FindFirstValue(CustomClaimTypes.SchoolId);
        return Guid.TryParse(schoolClaim, out var id) ? id : null;
    }
}

