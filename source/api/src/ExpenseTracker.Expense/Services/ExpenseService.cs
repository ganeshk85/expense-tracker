using System.Text.Json;
using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Services;
using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;
using ExpenseShareEntity = ExpenseTracker.Ocr.Entities.ExpenseShare;
using ReceiptEntity = ExpenseTracker.Receipt.Entities.Receipt;

namespace ExpenseTracker.Expense.Services;

public sealed class ExpenseService(
    IExpenseManagementRepository repo,
    IAuditService auditService,
    ILogger<ExpenseService> logger) : IExpenseService
{
    private const string AdminRole = "Admin";
    private const string ContributorRole = "Contributor";

    // ── Expense CRUD ─────────────────────────────────────────────────────────

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
        return await BuildResponseAsync(expense, ct);
    }

    public async Task<ExpenseListResponse> ListAsync(
        Guid userId, string userRole, bool allHousehold,
        int page, int pageSize, CancellationToken ct = default)
    {
        Guid? filterUserId = (allHousehold && userRole == AdminRole) ? null : userId;
        string? filterRole = filterUserId.HasValue ? userRole : null;

        var (items, total) = await repo.ListAsync(filterUserId, filterRole, page, pageSize, ct);

        var responses = new List<ExpenseResponse>(items.Count);
        foreach (var e in items)
            responses.Add(await BuildResponseAsync(e, ct));

        return new ExpenseListResponse(responses.AsReadOnly(), total, page, pageSize);
    }

    public async Task<ExpenseResponse> GetByIdAsync(
        Guid id, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);
        return await BuildResponseAsync(expense, ct);
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

        var expense = await repo.FindByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        // If total changed and shares exist, signal the FE to re-split.
        if (request.Total.HasValue &&
            expense.Total != request.Total &&
            expense.IsShared &&
            expense.Shares.Count > 0)
        {
            throw new ConflictException("shares_out_of_sync");
        }

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
        return await BuildResponseAsync(expense, ct);
    }

    public async Task DeleteAsync(
        Guid id, Guid userId, string userRole, string ipAddress, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        var beforeJson = JsonSerializer.Serialize(await BuildResponseAsync(expense, ct));

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

        var expense = await repo.FindByIdTrackedAsync(id, ct)
            ?? throw new NotFoundException("Expense", id);

        EnforceOwnership(expense, userId, userRole);

        var beforeJson = JsonSerializer.Serialize(await BuildResponseAsync(expense, ct));

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

        var afterJson = JsonSerializer.Serialize(await BuildResponseAsync(expense, ct));

        await auditService.LogAsync(new WriteAuditLogRequest(
            UserId: userId,
            Action: "EXPENSE_CORRECTION",
            ResourceType: "EXPENSE",
            ResourceId: id,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            IpAddress: ipAddress), ct);

        logger.LogInformation("Expense {Id} corrections applied by user {UserId}", id, userId);
        return await BuildResponseAsync(expense, ct);
    }

    // ── Item CRUD ────────────────────────────────────────────────────────────

    public async Task<ExpenseItemsListResponse> GetItemsAsync(
        Guid expenseId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        var items = await repo.GetItemsByExpenseIdAsync(expenseId, ct);
        return new ExpenseItemsListResponse(
            items.Select(i => new ExpenseItemResponse(i.Id, i.Name, i.Quantity, i.UnitPrice))
                 .ToList()
                 .AsReadOnly());
    }

    public async Task<ExpenseItemResponse> AddItemAsync(
        Guid expenseId, CreateExpenseItemRequest request,
        Guid userId, string userRole, CancellationToken ct = default)
    {
        ValidateItemRequest(request.Name, request.Quantity, request.UnitPrice);

        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        var item = new ExpenseItemEntity
        {
            ExpenseId = expenseId,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
        };

        await repo.AddItemAsync(item, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Item {ItemId} added to expense {ExpenseId} by user {UserId}",
            item.Id, expenseId, userId);

        return new ExpenseItemResponse(item.Id, item.Name, item.Quantity, item.UnitPrice);
    }

    public async Task<ExpenseItemResponse> UpdateItemAsync(
        Guid expenseId, Guid itemId, CreateExpenseItemRequest request,
        Guid userId, string userRole, CancellationToken ct = default)
    {
        ValidateItemRequest(request.Name, request.Quantity, request.UnitPrice);

        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        var item = await repo.FindItemByIdTrackedAsync(itemId, expenseId, ct)
            ?? throw new NotFoundException("ExpenseItem", itemId);

        item.Name = request.Name.Trim();
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;

        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Item {ItemId} on expense {ExpenseId} updated by user {UserId}",
            itemId, expenseId, userId);

        return new ExpenseItemResponse(item.Id, item.Name, item.Quantity, item.UnitPrice);
    }

    public async Task DeleteItemAsync(
        Guid expenseId, Guid itemId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        var item = await repo.FindItemByIdTrackedAsync(itemId, expenseId, ct)
            ?? throw new NotFoundException("ExpenseItem", itemId);

        await repo.RemoveItemAsync(item, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Item {ItemId} deleted from expense {ExpenseId} by user {UserId}",
            itemId, expenseId, userId);
    }

    // ── Shared Expenses ──────────────────────────────────────────────────────

    public async Task<ExpenseResponse> AssignSharesAsync(
        Guid expenseId, AssignSharesRequest request,
        Guid userId, string userRole, CancellationToken ct = default)
    {
        if (request.Shares.Count == 0)
            throw new ValidationException("At least one share must be provided.");

        var expense = await repo.FindByIdTrackedAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        // Validate: all entries use the same split type (all amounts or all percentages).
        bool allAmounts = request.Shares.All(s => s.Amount.HasValue);
        bool allPercentages = request.Shares.All(s => s.Percentage.HasValue);

        if (!allAmounts && !allPercentages)
            throw new ValidationException("All shares must specify either amount or percentage — not a mix.");

        if (allAmounts)
        {
            var total = request.Shares.Sum(s => s.Amount!.Value);
            if (Math.Abs(total - (expense.Total ?? 0)) > 0.01m)
                throw new ValidationException($"Share amounts ({total:F2}) must sum to the expense total ({expense.Total:F2}).");
        }
        else
        {
            var totalPct = request.Shares.Sum(s => s.Percentage!.Value);
            if (Math.Abs(totalPct - 100m) > 0.01m)
                throw new ValidationException($"Share percentages must sum to 100 (got {totalPct:F2}).");
        }

        var shares = request.Shares.Select(s => new ExpenseShareEntity
        {
            ExpenseId = expenseId,
            UserId = s.UserId,
            Amount = s.Amount,
            Percentage = s.Percentage,
        }).ToList();

        await repo.ReplaceSharesAsync(expenseId, shares, ct);

        expense.IsShared = true;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Expense {ExpenseId} shares assigned by user {UserId}", expenseId, userId);
        return await BuildResponseAsync(expense, ct);
    }

    // ── Receipt Attachment ───────────────────────────────────────────────────

    public async Task<ExpenseResponse> AttachReceiptAsync(
        Guid expenseId, Guid receiptId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        var receipt = await repo.FindReceiptByIdTrackedAsync(receiptId, ct)
            ?? throw new NotFoundException("Receipt", receiptId);

        if (receipt.UploadedByUserId != userId && userRole != AdminRole)
            throw new ForbiddenException("You can only attach receipts you uploaded.");

        if (receipt.ExpenseId.HasValue && receipt.ExpenseId != expenseId)
            throw new ConflictException("Receipt is already attached to a different expense.");

        receipt.ExpenseId = expenseId;
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Receipt {ReceiptId} attached to expense {ExpenseId} by user {UserId}",
            receiptId, expenseId, userId);

        return await BuildResponseAsync(expense, ct);
    }

    public async Task DetachReceiptAsync(
        Guid expenseId, Guid receiptId, Guid userId, string userRole, CancellationToken ct = default)
    {
        var expense = await repo.FindByIdAsync(expenseId, ct)
            ?? throw new NotFoundException("Expense", expenseId);

        EnforceOwnership(expense, userId, userRole);

        // Prevent detaching the primary OCR receipt.
        if (expense.ReceiptId == receiptId)
            throw new ValidationException("Cannot detach the primary receipt. Delete the expense instead.");

        var receipt = await repo.FindReceiptByIdTrackedAsync(receiptId, ct)
            ?? throw new NotFoundException("Receipt", receiptId);

        if (receipt.ExpenseId != expenseId)
            throw new ValidationException("Receipt is not attached to this expense.");

        receipt.ExpenseId = null;
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Receipt {ReceiptId} detached from expense {ExpenseId} by user {UserId}",
            receiptId, expenseId, userId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void EnforceOwnership(ExpenseEntity expense, Guid userId, string userRole)
    {
        if (userRole == AdminRole) return;
        if (expense.UserId == userId) return;
        // Contributor can access a shared expense they are a participant of.
        if (userRole == ContributorRole && expense.IsShared &&
            expense.Shares.Any(s => s.UserId == userId)) return;
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

    private static void ValidateItemRequest(string name, decimal quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Item name is required.");
        if (quantity <= 0)
            throw new ValidationException("Item quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new ValidationException("Item unit price cannot be negative.");
    }

    private async Task<ExpenseResponse> BuildResponseAsync(ExpenseEntity e, CancellationToken ct)
    {
        var receipts = await repo.GetReceiptsByExpenseIdAsync(e.Id, ct);
        return ToResponse(e, receipts);
    }

    private static ExpenseResponse ToResponse(ExpenseEntity e, IReadOnlyList<ReceiptEntity> receipts) => new(
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
        e.IsShared,
        e.Shares.Select(s => new ExpenseShareResponse(s.Id, s.UserId, s.Amount, s.Percentage))
                .ToList()
                .AsReadOnly(),
        receipts.Select(r => new ReceiptSummaryResponse(
            r.Id,
            r.ThumbnailPath is not null ? $"/receipts/{r.Id}/thumbnail" : null,
            r.Status.ToString()))
            .ToList()
            .AsReadOnly(),
        e.CreatedAt,
        e.UpdatedAt);
}
