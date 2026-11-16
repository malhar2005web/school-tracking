using SchoolTrack.Domain.Common;

namespace SchoolTrack.Domain.Entities;

/// <summary>
/// Represents an authenticated platform or school tenant user (Administrators, Drivers, Parents).
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>
    /// Nullable for platform SuperAdmin; required for school-scoped tenant users.
    /// </summary>
    public Guid? SchoolId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public School? School { get; set; }
    public Parent? ParentProfile { get; set; }
    public Driver? DriverProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

