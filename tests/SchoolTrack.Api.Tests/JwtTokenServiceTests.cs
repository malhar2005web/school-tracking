using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Authentication;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies JWT generation, claim mapping, token expiration, and refresh token hashing.
/// </summary>
public sealed class JwtTokenServiceTests
{
    private readonly JwtSettings _settings = new()
    {
        SecretKey = "SuperSecretTestingKeyAtLeast32BytesLength123456!",
        Issuer = "SchoolTrack.TestIssuer",
        Audience = "SchoolTrack.TestAudience",
        AccessTokenExpirationMinutes = 30,
        RefreshTokenExpirationDays = 15
    };

    private readonly IDateTimeProvider _dateTimeProvider = new StubDateTimeProvider();

    private JwtTokenService CreateService() =>
        new(Options.Create(_settings), _dateTimeProvider);

    [Fact]
    public void GenerateTokens_IncludesExpectedClaims()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            SchoolId = schoolId,
            Email = "admin@springfield.edu",
            FullName = "Seymour Skinner",
            Role = SystemRoles.SchoolAdmin,
            IsActive = true
        };

        var response = service.GenerateTokens(user, "Springfield Elementary");

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal(_dateTimeProvider.UtcNow.AddMinutes(30), response.ExpiresAtUtc);
        Assert.Equal(user.Email, response.User.Email);
        Assert.Equal(schoolId, response.User.SchoolId);
        Assert.Equal("Springfield Elementary", response.User.SchoolName);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.AccessToken);

        Assert.Equal(_settings.Issuer, jwt.Issuer);
        Assert.Contains(_settings.Audience, jwt.Audiences);

        var subClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        var emailClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;
        var schoolClaim = jwt.Claims.First(c => c.Type == CustomClaimTypes.SchoolId).Value;
        var roleClaim = jwt.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role || c.Type == CustomClaimTypes.Role).Value;

        Assert.Equal(userId.ToString(), subClaim);
        Assert.Equal(user.Email, emailClaim);
        Assert.Equal(schoolId.ToString(), schoolClaim);
        Assert.Equal(SystemRoles.SchoolAdmin, roleClaim);
    }

    [Fact]
    public void HashRefreshToken_ProducesDeterministicSha256Hex()
    {
        var service = CreateService();
        var rawToken = "sample-high-entropy-token-12345";

        var hash1 = service.HashRefreshToken(rawToken);
        var hash2 = service.HashRefreshToken(rawToken);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex characters
    }

    [Fact]
    public void GenerateSecureRefreshToken_ProducesNonEmptyUrlSafeToken()
    {
        var service = CreateService();
        var token1 = service.GenerateSecureRefreshToken();
        var token2 = service.GenerateSecureRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.False(token1.Contains('+'));
        Assert.False(token1.Contains('/'));
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
    }
}
