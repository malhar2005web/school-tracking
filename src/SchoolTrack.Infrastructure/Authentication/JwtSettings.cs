namespace SchoolTrack.Infrastructure.Authentication;

/// <summary>
/// Configuration options governing JWT access token generation and cryptographic signing.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = "SchoolTrack_DefaultDevSecretKey_ChangeInProduction_AtLeast32BytesLong!";
    public string Issuer { get; set; } = "SchoolTrack.Api";
    public string Audience { get; set; } = "SchoolTrack.Clients";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}

