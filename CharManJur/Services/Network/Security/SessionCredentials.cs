using System.Security.Cryptography;
using System.Text;

namespace CharManJur.Services;

// What the host needs to check a session password without keeping the password itself.
// Key is PBKDF2-SHA256(password, Salt, Iterations): the player's device works out the same value from the
// password they type, and it is mixed into the encryption keys, so a wrong password simply cannot talk to the host.
// Store Salt + Iterations + Key with the campaign; never store the password, never send the Key.
public sealed class SessionCredentials
{
    public const int DefaultIterations = 200_000;
    public const int MinIterations = 1_000;       // a hostile host may not ask a phone for more than the max, or less than the min
    public const int MaxIterations = 1_000_000;
    public const int SaltBytes = 16;
    public const int KeyBytes = 32;

    public SessionCredentials(byte[] salt, byte[] key, int iterations)
    {
        if (salt == null || salt.Length is < 8 or > 64) throw new ArgumentException("The salt must be 8 to 64 bytes.", nameof(salt));
        if (key == null || key.Length != KeyBytes) throw new ArgumentException($"The key must be {KeyBytes} bytes.", nameof(key));
        if (iterations is < MinIterations or > MaxIterations) throw new ArgumentOutOfRangeException(nameof(iterations));

        Salt = salt;
        Key = key;
        Iterations = iterations;
    }

    public byte[] Salt { get; }
    public byte[] Key { get; }
    public int Iterations { get; }

    // Makes new credentials (a fresh random salt unless one is given).
    public static SessionCredentials FromPassword(string password, byte[]? salt = null, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("The password cannot be empty.", nameof(password));

        salt ??= RandomNumberGenerator.GetBytes(SaltBytes);
        return new SessionCredentials(salt, Derive(password, salt, iterations), iterations);
    }

    public static byte[] Derive(string password, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeyBytes);
}