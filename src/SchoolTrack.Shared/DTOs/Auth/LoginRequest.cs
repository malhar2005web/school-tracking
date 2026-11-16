namespace SchoolTrack.Shared.DTOs.Auth;

/// <summary>
/// Credential payload for user authentication.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password);

