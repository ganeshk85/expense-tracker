using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IExpenseService
{
    // ── Expense CRUD ─────────────────────────────────────────────────────────

    Task<ExpenseResponse> CreateManualAsync(
        CreateExpenseRequest request, Guid userId, CancellationToken ct = default);

    Task<ExpenseListResponse> ListAsync(
        Guid userId, string userRole, bool allHousehold,
        int page, int pageSize, CancellationToken ct = default);

    Task<ExpenseResponse> GetByIdAsync(
        Guid id, Guid userId, string userRole, CancellationToken ct = default);

    Task<ExpenseResponse> UpdateAsync(
        Guid id, UpdateExpenseRequest request,
        Guid userId, string userRole, CancellationToken ct = default);

    Task DeleteAsync(
        Guid id, Guid userId, string userRole, string ipAddress, CancellationToken ct = default);

    Task<ExpenseResponse> ApplyCorrectionsAsync(
        Guid id, CorrectExpenseRequest request,
        Guid userId, string userRole, string ipAddress, CancellationToken ct = default);

    // ── Item CRUD ────────────────────────────────────────────────────────────

    Task<ExpenseItemsListResponse> GetItemsAsync(
        Guid expenseId, Guid userId, string userRole, CancellationToken ct = default);

    Task<ExpenseItemResponse> AddItemAsync(
        Guid expenseId, CreateExpenseItemRequest request,
        Guid userId, string userRole, CancellationToken ct = default);

    Task<ExpenseItemResponse> UpdateItemAsync(
        Guid expenseId, Guid itemId, CreateExpenseItemRequest request,
        Guid userId, string userRole, CancellationToken ct = default);

    Task DeleteItemAsync(
        Guid expenseId, Guid itemId, Guid userId, string userRole, CancellationToken ct = default);

    // ── Shared Expenses ──────────────────────────────────────────────────────

    Task<ExpenseResponse> AssignSharesAsync(
        Guid expenseId, AssignSharesRequest request,
        Guid userId, string userRole, CancellationToken ct = default);

    // ── Receipt Attachment ───────────────────────────────────────────────────

    Task<ExpenseResponse> AttachReceiptAsync(
        Guid expenseId, Guid receiptId, Guid userId, string userRole, CancellationToken ct = default);

    Task DetachReceiptAsync(
        Guid expenseId, Guid receiptId, Guid userId, string userRole, CancellationToken ct = default);

    // ── File Attachments ─────────────────────────────────────────────────────

    Task<AttachmentListResponse> GetAttachmentsAsync(
        Guid expenseId, Guid userId, string userRole, CancellationToken ct = default);

    Task<ExpenseAttachmentResponse> AddAttachmentAsync(
        Guid expenseId, Microsoft.AspNetCore.Http.IFormFile file,
        Guid userId, string userRole, CancellationToken ct = default);

    Task DeleteAttachmentAsync(
        Guid expenseId, Guid attachmentId, Guid userId, string userRole, CancellationToken ct = default);

    // ── Search ───────────────────────────────────────────────────────────────

    Task<ExpenseListResponse> SearchAsync(
        SearchExpensesRequest request, Guid userId, string userRole, CancellationToken ct = default);
}
