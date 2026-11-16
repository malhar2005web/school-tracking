using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Infrastructure.Services;

/// <summary>
/// Accesses authenticated user identity from the ambient HTTP context.
/// </summary>
public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public Guid? SchoolId
    {
        get
        {
            var claim = User?.FindFirst(CustomClaimTypes.SchoolId)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    /// <inheritdoc />
    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value
        ?? User?.FindFirst(CustomClaimTypes.Role)?.Value;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}

