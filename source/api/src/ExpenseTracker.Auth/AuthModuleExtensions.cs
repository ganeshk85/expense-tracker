using ExpenseTracker.Auth.Repositories;
using ExpenseTracker.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Auth;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        // AesTotpEncryptor reads Mfa:EncryptionKey from IConfiguration at construction time.
        services.AddSingleton<ITotpEncryptor, AesTotpEncryptor>();
        return services;
    }
}
