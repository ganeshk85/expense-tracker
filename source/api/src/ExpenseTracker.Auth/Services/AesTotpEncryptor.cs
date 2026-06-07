using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.Auth.Services;

/// <summary>
/// AES-256-CBC implementation of <see cref="ITotpEncryptor"/>.
/// The key must be a 64-character hex string (32 bytes) configured at Mfa:EncryptionKey.
/// Each encryption generates a random IV prepended to the ciphertext (Base64-encoded).
/// </summary>
public sealed class AesTotpEncryptor : ITotpEncryptor
{
    private const int AesKeyBytes = 32;
    private const int AesIvBytes = 16;

    private readonly byte[] _key;

    public AesTotpEncryptor(IConfiguration configuration)
    {
        var hexKey = configuration["Mfa:EncryptionKey"]
            ?? throw new InvalidOperationException("Mfa:EncryptionKey is not configured. Set a 64-character hex string.");

        if (hexKey.Length != 64)
            throw new InvalidOperationException("Mfa:EncryptionKey must be exactly 64 hex characters (32 bytes).");

        _key = Convert.FromHexString(hexKey);
    }

    public string Encrypt(string plaintext)
    {
        var iv = RandomNumberGenerator.GetBytes(AesIvBytes);
        using var aes = CreateAes(iv);
        using var encryptor = aes.CreateEncryptor();

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Format: Base64(IV || ciphertext)
        var combined = new byte[AesIvBytes + ciphertextBytes.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, AesIvBytes);
        Buffer.BlockCopy(ciphertextBytes, 0, combined, AesIvBytes, ciphertextBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        var combined = Convert.FromBase64String(ciphertext);

        if (combined.Length < AesIvBytes)
            throw new CryptographicException("Ciphertext is too short to contain a valid IV.");

        var iv = combined[..AesIvBytes];
        var ciphertextBytes = combined[AesIvBytes..];

        using var aes = CreateAes(iv);
        using var decryptor = aes.CreateDecryptor();

        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private Aes CreateAes(byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = AesKeyBytes * 8;
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }
}
