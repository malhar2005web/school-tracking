namespace SchoolTrack.Shared.DTOs.Auth;

/// <summary>
/// Refresh token submission payload for rotating access credentials.
/// </summary>
public sealed record RefreshTokenRequest(
    string RefreshToken);

