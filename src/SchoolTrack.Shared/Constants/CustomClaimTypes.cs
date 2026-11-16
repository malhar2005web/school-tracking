namespace SchoolTrack.Shared.Constants;

/// <summary>
/// Claim type names used across JWT generation, identity validation, and tenant resolution.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Identifier of the school tenant to which the user belongs.
    /// </summary>
    public const string SchoolId = "school_id";

    /// <summary>
    /// User's display full name.
    /// </summary>
    public const string FullName = "full_name";

    /// <summary>
    /// Assigned RBAC system role name.
    /// </summary>
    public const string Role = "role";
}

