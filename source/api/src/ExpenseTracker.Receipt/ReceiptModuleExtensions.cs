using ExpenseTracker.Receipt.Repositories;
using ExpenseTracker.Receipt.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Receipt;

public static class ReceiptModuleExtensions
{
    public static IServiceCollection AddReceiptModule(this IServiceCollection services)
    {
        services.AddScoped<IReceiptService, ReceiptService>();
        return services;
    }
}
