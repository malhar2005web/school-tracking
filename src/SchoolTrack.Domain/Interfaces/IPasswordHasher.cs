namespace SchoolTrack.Domain.Interfaces;

/// <summary>
/// Cryptographic one-way hashing abstraction for credential verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Computes a salted one-way hash of the plaintext password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies whether the plaintext password matches the stored salted hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}

