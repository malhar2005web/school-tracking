using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Auth;

namespace SchoolTrack.Infrastructure.Authentication;

/// <summary>
/// Issues cryptographically signed JWT access tokens and cryptographically random rotating refresh tokens.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(
        IOptions<JwtSettings> settings,
        IDateTimeProvider dateTimeProvider)
    {
        _settings = settings.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public AuthResponse GenerateTokens(User user, string? schoolName = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = _dateTimeProvider.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(CustomClaimTypes.FullName, user.FullName),
            new(CustomClaimTypes.Role, user.Role)
        };

        if (user.SchoolId.HasValue)
        {
            claims.Add(new Claim(CustomClaimTypes.SchoolId, user.SchoolId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = _dateTimeProvider.UtcNow.UtcDateTime,
            IssuedAt = _dateTimeProvider.UtcNow.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(securityToken);

        var rawRefreshToken = GenerateSecureRefreshToken();

        var userDto = new UserDto(
            Id: user.Id,
            SchoolId: user.SchoolId,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role,
            IsActive: user.IsActive,
            SchoolName: schoolName ?? user.School?.Name);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAtUtc: expiresAtUtc,
            User: userDto);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc />
    public string GenerateSecureRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}

