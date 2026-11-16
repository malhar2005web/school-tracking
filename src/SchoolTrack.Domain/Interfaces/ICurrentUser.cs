namespace SchoolTrack.Domain.Interfaces;

/// <summary>
/// Provides identity, tenant, and role metadata for the current ambient caller.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? SchoolId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

