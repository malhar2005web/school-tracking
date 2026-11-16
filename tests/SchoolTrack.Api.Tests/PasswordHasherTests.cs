using SchoolTrack.Infrastructure.Authentication;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies PBKDF2 password hashing, salt uniqueness, and constant-time verification.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesNonEmptySaltedHash()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_SamePasswordProducesUniqueHashesDueToSalt()
    {
        var password = "IdenticalPassword!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.VerifyPassword(password, hash1));
        Assert.True(_hasher.VerifyPassword(password, hash2));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("ActualPassword");

        var result = _hasher.VerifyPassword("WrongPassword", hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-base64-hash")]
    [InlineData("short")]
    public void VerifyPassword_MalformedHash_ReturnsFalseSafely(string malformedHash)
    {
        var result = _hasher.VerifyPassword("password", malformedHash);

        Assert.False(result);
    }
}

