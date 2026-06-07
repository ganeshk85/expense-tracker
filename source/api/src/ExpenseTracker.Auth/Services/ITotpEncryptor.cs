namespace ExpenseTracker.Auth.Services;

/// <summary>
/// Encrypts and decrypts TOTP secrets stored in the database.
/// Uses AES-256-CBC with a key loaded from application configuration.
/// </summary>
public interface ITotpEncryptor
{
    /// <summary>Encrypts a base32-encoded TOTP secret for storage.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a stored encrypted TOTP secret.</summary>
    string Decrypt(string ciphertext);
}
