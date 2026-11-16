using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Infrastructure.Services;

/// <summary>
/// Resolves active tenant isolation context from ambient HTTP request claims and headers.
/// </summary>
public sealed class HttpContextTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid? SchoolId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var claim = user.FindFirst(CustomClaimTypes.SchoolId)?.Value;
                if (Guid.TryParse(claim, out var schoolId))
                {
                    return schoolId;
                }
            }

            // Fallback for public gateway or device endpoints if provided via header
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-School-Id", out var headerValue) == true)
            {
                if (Guid.TryParse(headerValue.FirstOrDefault(), out var headerSchoolId))
                {
                    return headerSchoolId;
                }
            }

            return null;
        }
    }

    /// <inheritdoc />
    public bool IsPlatformAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return user.IsInRole(SystemRoles.SuperAdmin);
        }
    }
}

