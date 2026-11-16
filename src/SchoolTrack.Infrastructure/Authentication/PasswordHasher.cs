using System.Security.Cryptography;
using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.Infrastructure.Authentication;

/// <summary>
/// Implements salted PBKDF2 password hashing with SHA-256 and constant-time verification.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        var outputBytes = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, outputBytes, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, outputBytes, SaltSize, HashSize);

        return Convert.ToBase64String(outputBytes);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            if (hashBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            var storedHash = new byte[HashSize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedHash, 0, HashSize);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }
}

