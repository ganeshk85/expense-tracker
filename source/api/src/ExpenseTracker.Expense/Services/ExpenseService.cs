using System.Text.Json;
using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Services;
using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;

namespace ExpenseTracker.Expense.Services;

public sealed class ExpenseService(
    IExpenseManagementRepository repo,
    IAuditService auditService,
    ILogger<ExpenseService> logger) : IExpenseService
{
    private const string OwnerRole = "Owner";

    public async Task<ExpenseResponse> CreateManualAsync(
        CreateExpenseRequest request, Guid userId, CancellationToken ct = default)
    {
        if (request.Total <= 0)
            throw new ValidationException("Amount must be greater than zero.");

        if (request.Date.HasValue && request.Date.Value > DateTimeOffset.UtcNow)
            throw new ValidationException("Expense date cannot be in the future.");

        if (!ExpenseCategory.IsValid(request.Category))
            throw new ValidationException($"Invalid category '{request.Category}'.");

        var expense = new ExpenseEntity
        {
            ReceiptId = null,
            UserId = userId,
            MerchantName = request.MerchantName,
            Date = request.Date,
            Total = request.Total,
            Category = request.Category,
            Tags = request.Tags ?? [],
            Notes = request.Notes,
            OcrStatus = ExpenseTracker.Ocr.Entities.OcrStatusValue.Manual,
            Source = ExpenseTracker.Ocr.Entities.ExpenseSource.Manual,
        };

        await repo.AddAsync(expense, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Manual expense {Id} created for user {UserId}", expense.Id, userId);
        return ToResponse(expense);
    }

    public async Task<ExpenseListResponse> ListAsync(
        Guid userId, string userRole, bool allHousehold,
        int page, int pageSize, CancellationToken ct = default)
    {
        Guid? filterUserId = (allHousehold && userRole == OwnerRole) ? null : userId;

        var (items, total) = await repo.ListAsync(filterUserId, page, pageSize, ct);
        return new ExpenseListResponse(
            items.Select(ToResponse).ToList().AsReadOnly(),
            total, page, pageSize);
    }

    public async Task<ExpenseResponse> GetByIdAsync(
        Guid id, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);
        return ToResponse(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(
        Guid id, UpdateExpenseRequest request,
        Guid userId, string userRole, CancellationToken ct = default)
    {
        if (request.Total.HasValue && request.Total <= 0)
            throw new ValidationException("Amount must be greater than zero.");

        if (request.Date.HasValue && request.Date.Value > DateTimeOffset.UtcNow)
            throw new ValidationException("Expense date cannot be in the future.");

        if (!ExpenseCategory.IsValid(request.Category))
            throw new ValidationException($"Invalid category '{request.Category}'.");

        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        expense.MerchantName = request.MerchantName;
        expense.MerchantAddress = request.MerchantAddress;
        expense.Date = request.Date;
        expense.Time = request.Time;
        expense.Subtotal = request.Subtotal;
        expense.TaxAmount = request.TaxAmount;
        expense.Total = request.Total;
        expense.Category = request.Category;
        expense.Tags = request.Tags ?? [];
        expense.Notes = request.Notes;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Items is not null)
            ReplaceItems(expense, request.Items);

        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Expense {Id} updated by user {UserId}", id, userId);
        return ToResponse(expense);
    }

    public async Task DeleteAsync(
        Guid id, Guid userId, string userRole, string ipAddress, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        var beforeJson = JsonSerializer.Serialize(ToResponse(expense));

        await repo.DeleteAsync(expense, ct);
        await repo.SaveChangesAsync(ct);

        await auditService.LogAsync(new WriteAuditLogRequest(
            UserId: userId,
            Action: "EXPENSE_DELETE",
            ResourceType: "EXPENSE",
            ResourceId: id,
            BeforeJson: beforeJson,
            AfterJson: null,
            IpAddress: ipAddress), ct);

        logger.LogInformation("Expense {Id} deleted by user {UserId}", id, userId);
    }

    public async Task<ExpenseResponse> ApplyCorrectionsAsync(
        Guid id, CorrectExpenseRequest request,
        Guid userId, string userRole, string ipAddress, CancellationToken ct = default)
    {
        if (request.Total.HasValue && request.Total <= 0)
            throw new ValidationException("Amount must be greater than zero.");

        if (request.Date.HasValue && request.Date.Value > DateTimeOffset.UtcNow)
            throw new ValidationException("Expense date cannot be in the future.");

        if (!ExpenseCategory.IsValid(request.Category))
            throw new ValidationException($"Invalid category '{request.Category}'.");

        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        var beforeJson = JsonSerializer.Serialize(ToResponse(expense));

        if (request.MerchantName is not null)
            expense.MerchantName = request.MerchantName;
        if (request.Date.HasValue)
            expense.Date = request.Date;
        if (request.Total.HasValue)
            expense.Total = request.Total;
        if (request.Subtotal.HasValue)
            expense.Subtotal = request.Subtotal;
        if (request.TaxAmount.HasValue)
            expense.TaxAmount = request.TaxAmount;
        if (request.Category is not null)
            expense.Category = request.Category;
        if (request.Tags is not null)
            expense.Tags = request.Tags;
        if (request.Notes is not null)
            expense.Notes = request.Notes;
        if (request.Items is not null)
            ReplaceItems(expense, request.Items);

        // Clear confidence JSON once the user confirms the corrections.
        expense.ConfidenceJson = null;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await repo.SaveChangesAsync(ct);

        var afterJson = JsonSerializer.Serialize(ToResponse(expense));

        await auditService.LogAsync(new WriteAuditLogRequest(
            UserId: userId,
            Action: "EXPENSE_CORRECTION",
            ResourceType: "EXPENSE",
            ResourceId: id,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            IpAddress: ipAddress), ct);

        logger.LogInformation("Expense {Id} corrections applied by user {UserId}", id, userId);
        return ToResponse(expense);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static void EnforceOwnership(ExpenseEntity expense, Guid userId, string userRole)
    {
        if (userRole != OwnerRole && expense.UserId != userId)
            throw new ForbiddenException("You do not have access to this expense.");
    }

    private static void ReplaceItems(
        ExpenseEntity expense, IReadOnlyList<UpdateExpenseItemRequest> items)
    {
        expense.Items = items
            .Select(r => new ExpenseItemEntity
            {
                Id = r.Id ?? Guid.NewGuid(),
                ExpenseId = expense.Id,
                Name = r.Name,
                Quantity = r.Quantity,
                UnitPrice = r.UnitPrice,
            })
            .ToList();
    }

    private static ExpenseResponse ToResponse(ExpenseEntity e) => new(
        e.Id,
        e.ReceiptId,
        e.UserId,
        e.MerchantName,
        e.MerchantAddress,
        e.Date,
        e.Time,
        e.Subtotal,
        e.TaxAmount,
        e.Total,
        e.Category,
        e.Tags,
        e.Notes,
        e.Source,
        e.OcrStatus,
        e.ConfidenceJson,
        e.Items.Select(i => new ExpenseItemResponse(i.Id, i.Name, i.Quantity, i.UnitPrice))
               .ToList()
               .AsReadOnly(),
        e.CreatedAt,
        e.UpdatedAt);
}
