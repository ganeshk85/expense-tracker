using ExpenseTracker.Ocr.Repositories;
using ExpenseTracker.Ocr.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Ocr;

public static class OcrModuleExtensions
{
    /// <summary>
    /// Registers OCR module services. IExpenseRepository implementation must be
    /// registered separately in the host project (it depends on AppDbContext).
    /// </summary>
    public static IServiceCollection AddOcrModule(this IServiceCollection services)
    {
        services.AddHostedService<OcrResultConsumerService>();
        return services;
    }
}
