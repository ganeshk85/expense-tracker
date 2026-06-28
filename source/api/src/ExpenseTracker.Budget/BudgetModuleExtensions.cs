using ExpenseTracker.Budget.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Budget;

public static class BudgetModuleExtensions
{
    /// <summary>
    /// Registers Budget module services. IBudgetRepository must be registered separately
    /// in the host project (which has access to AppDbContext).
    /// </summary>
    public static IServiceCollection AddBudgetModule(this IServiceCollection services)
    {
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddHostedService<BudgetResetService>();
        return services;
    }
}
