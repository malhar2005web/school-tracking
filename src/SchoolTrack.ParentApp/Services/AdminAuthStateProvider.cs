using SchoolTrack.Admin.Models;

namespace SchoolTrack.Admin.Services;

public class AdminAuthStateProvider
{
    private AuthSession _currentSession;
    public event Action? OnChange;

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
    public bool IsAuthenticated => _currentSession.IsAuthenticated;

    public void SetSession(AuthSession session)
    {
        _currentSession = session;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        _currentSession = new AuthSession
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            Role = string.Empty,
            SchoolName = string.Empty
        };
        OnChange?.Invoke();
    }
}
