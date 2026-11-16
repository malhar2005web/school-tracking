using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SchoolTrack.Admin.Models;

namespace SchoolTrack.Admin.Services;

public class AdminAuthStateProvider : AuthenticationStateProvider
{
    private AuthSession _currentSession;

    public AdminAuthStateProvider()
    {
        // Initial default demo session for Oakridge Academy District #4
        _currentSession = new AuthSession
        {
            AccessToken = "demo-jwt-token-oakridge-admin",
            RefreshToken = "demo-refresh-token",
            UserId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            FullName = "Elena Vasquez",
            Email = "elena.vasquez@oakridge.edu",
            Role = "SchoolAdmin",
            SchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SchoolName = "Oakridge Academy District #4"
        };
    }

    public AuthSession CurrentSession => _currentSession;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_currentSession.IsAuthenticated)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentSession.UserId.ToString()),
            new Claim(ClaimTypes.Name, _currentSession.FullName),
            new Claim(ClaimTypes.Email, _currentSession.Email),
            new Claim(ClaimTypes.Role, _currentSession.Role),
            new Claim("school_id", _currentSession.SchoolId.ToString()),
            new Claim("school_name", _currentSession.SchoolName)
        };

        var identity = new ClaimsIdentity(claims, "SchoolTrackJwtAuth");
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    public void SetSession(AuthSession session)
    {
        _currentSession = session;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Logout()
    {
        _currentSession = new AuthSession
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty
        };
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

