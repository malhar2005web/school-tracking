using SchoolTrack.Domain.Entities;
using SchoolTrack.Shared.DTOs.Auth;

namespace SchoolTrack.Domain.Interfaces;

/// <summary>
/// Issues JWT access tokens and cryptographically random rotating refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT bearer token and raw refresh token for an authenticated user.
    /// </summary>
    AuthResponse GenerateTokens(User user, string? schoolName = null);

    /// <summary>
    /// Produces a deterministic SHA-256 hash of a raw refresh token for safe database persistence.
    /// </summary>
    string HashRefreshToken(string rawToken);

    /// <summary>
    /// Generates a cryptographically strong, high-entropy random refresh token string.
    /// </summary>
    string GenerateSecureRefreshToken();
}

