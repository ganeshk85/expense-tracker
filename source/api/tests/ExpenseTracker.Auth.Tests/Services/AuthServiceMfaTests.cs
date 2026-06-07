using ExpenseTracker.Auth.Entities;
using ExpenseTracker.Auth.Models;
using ExpenseTracker.Auth.Repositories;
using ExpenseTracker.Auth.Services;
using ExpenseTracker.Shared.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OtpNet;
using Xunit;

namespace ExpenseTracker.Auth.Tests.Services;

public sealed class AuthServiceMfaTests
{
    // 64-char hex key for AES-256 test encryption
    private const string TestHexKey = "AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899";

    private static User MakeUser(bool mfaEnabled = false, string? totpSecretEncrypted = null) => new()
    {
        Username = "testuser",
        PasswordHash = "fakehash",
        Role = UserRole.AdultMember,
        MfaEnabled = mfaEnabled,
        TotpSecretEncrypted = totpSecretEncrypted
    };

    private static (AuthService service, Mock<IUserRepository> usersMock) CreateService(User user)
    {
        var usersMock = new Mock<IUserRepository>();
        usersMock.Setup(r => r.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var invitesMock = new Mock<IInviteTokenRepository>();
        var hasherMock = new Mock<IPasswordHasher>();

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mfa:EncryptionKey"] = TestHexKey
            })
            .Build();

        var encryptor = new AesTotpEncryptor(config);

        var service = new AuthService(
            usersMock.Object,
            invitesMock.Object,
            hasherMock.Object,
            encryptor,
            NullLogger<AuthService>.Instance);

        return (service, usersMock);
    }

    [Fact]
    public async Task SetupMfaAsync_ReturnsSecretAndUri_WithoutPersisting()
    {
        var user = MakeUser();
        var (service, usersMock) = CreateService(user);

        var result = await service.SetupMfaAsync(user.Id);

        result.Secret.Should().NotBeNullOrEmpty();
        result.OtpAuthUri.Should().StartWith("otpauth://totp/ExpenseTracker:");
        result.OtpAuthUri.Should().Contain(result.Secret);

        // Must NOT have saved anything to DB during setup.
        usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableMfaAsync_ValidOtp_SetsMfaEnabledAndEncryptsSecret()
    {
        var user = MakeUser();
        var (service, usersMock) = CreateService(user);

        // Generate a real TOTP secret and compute a valid OTP.
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var request = new MfaEnableRequest(base32Secret, validCode);
        await service.EnableMfaAsync(user.Id, request);

        user.MfaEnabled.Should().BeTrue();
        user.TotpSecretEncrypted.Should().NotBeNullOrEmpty();
        user.TotpSecretEncrypted.Should().NotBe(base32Secret, "secret must be stored encrypted");
        usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnableMfaAsync_InvalidOtp_ThrowsValidationException()
    {
        var user = MakeUser();
        var (service, _) = CreateService(user);

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);
        var request = new MfaEnableRequest(base32Secret, "000000");

        var act = async () => await service.EnableMfaAsync(user.Id, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid OTP*");

        user.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyMfaLoginAsync_ValidOtp_ReturnsRole()
    {
        var user = MakeUser();
        var (service, _) = CreateService(user);

        // First enable MFA to populate the encrypted secret.
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);
        var setupTotp = new Totp(secretBytes);
        var setupCode = setupTotp.ComputeTotp();
        await service.EnableMfaAsync(user.Id, new MfaEnableRequest(base32Secret, setupCode));

        // Now verify login with a fresh code.
        var loginCode = setupTotp.ComputeTotp();
        var role = await service.VerifyMfaLoginAsync(user.Id, new MfaLoginRequest(loginCode));

        role.Should().Be("AdultMember");
    }

    [Fact]
    public async Task VerifyMfaLoginAsync_InvalidOtp_ThrowsUnauthorizedException()
    {
        var user = MakeUser();
        var (service, _) = CreateService(user);

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);
        var setupTotp = new Totp(secretBytes);
        var setupCode = setupTotp.ComputeTotp();
        await service.EnableMfaAsync(user.Id, new MfaEnableRequest(base32Secret, setupCode));

        var act = async () => await service.VerifyMfaLoginAsync(user.Id, new MfaLoginRequest("000000"));

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid MFA code*");
    }

    [Fact]
    public async Task VerifyMfaLoginAsync_MfaNotEnabled_ThrowsValidationException()
    {
        var user = MakeUser(mfaEnabled: false);
        var (service, _) = CreateService(user);

        var act = async () => await service.VerifyMfaLoginAsync(user.Id, new MfaLoginRequest("123456"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*MFA is not enabled*");
    }

    [Fact]
    public async Task AdminToggleMfaAsync_Disable_ClearsMfaAndSecret()
    {
        var user = MakeUser(mfaEnabled: true, totpSecretEncrypted: "someciphertext");
        var (service, usersMock) = CreateService(user);

        await service.AdminToggleMfaAsync(user.Id, enabled: false);

        user.MfaEnabled.Should().BeFalse();
        user.TotpSecretEncrypted.Should().BeNull();
        usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminToggleMfaAsync_Enable_SetsMfaFlag()
    {
        var user = MakeUser(mfaEnabled: false);
        var (service, usersMock) = CreateService(user);

        await service.AdminToggleMfaAsync(user.Id, enabled: true);

        user.MfaEnabled.Should().BeTrue();
        usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminToggleMfaAsync_UserNotFound_ThrowsNotFoundException()
    {
        var usersMock = new Mock<IUserRepository>();
        usersMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mfa:EncryptionKey"] = TestHexKey
            })
            .Build();

        var service = new AuthService(
            usersMock.Object,
            new Mock<IInviteTokenRepository>().Object,
            new Mock<IPasswordHasher>().Object,
            new AesTotpEncryptor(config),
            NullLogger<AuthService>.Instance);

        var act = async () => await service.AdminToggleMfaAsync(Guid.NewGuid(), enabled: false);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
