using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Extensions;

/// <summary>
/// Configures role-based and multi-tenant authorization policies.
/// </summary>
public static class AuthorizationExtensions
{
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string RequireSchoolAdmin = "RequireSchoolAdmin";
    public const string RequireTransportStaff = "RequireTransportStaff";
    public const string RequireDriver = "RequireDriver";
    public const string RequireParent = "RequireParent";

    /// <summary>
    /// Registers RBAC authorization policies for SchoolTrack actors.
    /// </summary>
    public static IServiceCollection AddSchoolTrackAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(RequireSuperAdmin, policy =>
                policy.RequireRole(SystemRoles.SuperAdmin));

            options.AddPolicy(RequireSchoolAdmin, policy =>
                policy.RequireRole(SystemRoles.SuperAdmin, SystemRoles.SchoolAdmin));

            options.AddPolicy(RequireTransportStaff, policy =>
                policy.RequireRole(SystemRoles.SuperAdmin, SystemRoles.SchoolAdmin, SystemRoles.TransportManager));

            options.AddPolicy(RequireDriver, policy =>
                policy.RequireRole(SystemRoles.Driver));

            options.AddPolicy(RequireParent, policy =>
                policy.RequireRole(SystemRoles.Parent));
        });

        return services;
    }
}

