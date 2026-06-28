using ExpenseTracker.Expense.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Expense;

public static class ExpenseModuleExtensions
{
    public static IServiceCollection AddExpenseModule(this IServiceCollection services)
    {
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
