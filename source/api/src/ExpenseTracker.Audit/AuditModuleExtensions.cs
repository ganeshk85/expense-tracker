using ExpenseTracker.Audit.Middleware;
using ExpenseTracker.Audit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Audit;

public static class AuditModuleExtensions
{
    /// <summary>
    /// Registers audit module services. Call from Program.cs.
    /// IAuditRepository must be registered separately in the host project
    /// because its implementation (AuditRepository) depends on AppDbContext.
    /// </summary>
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        // Singleton so the fire-and-forget task scheduler has a stable scope factory reference.
        services.AddSingleton<IAuditService, AuditService>();
        return services;
    }

    public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<AuditMiddleware>();
}
