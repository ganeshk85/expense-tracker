using ExpenseTracker.Auth.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExpenseTracker.Auth.Tests.Services;

public sealed class AesTotpEncryptorTests
{
    private static ITotpEncryptor CreateEncryptor(string? hexKey = null)
    {
        // 64 hex chars = 32 bytes = AES-256
        var key = hexKey ?? "AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mfa:EncryptionKey"] = key
            })
            .Build();
        return new AesTotpEncryptor(config);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var encryptor = CreateEncryptor();
        const string original = "JBSWY3DPEHPK3PXP";

        var ciphertext = encryptor.Encrypt(original);
        var decrypted = encryptor.Decrypt(ciphertext);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_SameInputTwice_ProducesDifferentCiphertexts()
    {
        var encryptor = CreateEncryptor();
        const string secret = "JBSWY3DPEHPK3PXP";

        var first = encryptor.Encrypt(secret);
        var second = encryptor.Encrypt(secret);

        // Random IV means each encryption is unique.
        first.Should().NotBe(second);
    }

    [Fact]
    public void Constructor_MissingKey_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => new AesTotpEncryptor(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Mfa:EncryptionKey*");
    }

    [Fact]
    public void Constructor_KeyWrongLength_ThrowsInvalidOperationException()
    {
        var act = () => CreateEncryptor("TOOSHORT");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*64 hex characters*");
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var encryptor = CreateEncryptor();
        var ciphertext = encryptor.Encrypt("JBSWY3DPEHPK3PXP");

        // Corrupt a byte in the ciphertext.
        var bytes = Convert.FromBase64String(ciphertext);
        bytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => encryptor.Decrypt(tampered);

        act.Should().Throw<Exception>();
    }
}
