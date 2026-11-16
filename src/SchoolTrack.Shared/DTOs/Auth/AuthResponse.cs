namespace SchoolTrack.Shared.DTOs.Auth;

/// <summary>
/// Authentication response containing JWT bearer access token and rotation token.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    UserDto User);

