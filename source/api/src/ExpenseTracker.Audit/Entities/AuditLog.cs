namespace ExpenseTracker.Audit.Entities;

/// <summary>
/// Append-only audit log record. Enforced at the DB level via PostgreSQL RLS policy:
/// only INSERT is allowed; UPDATE and DELETE are denied for all roles.
/// The Owner-facing GET /audit endpoint reads using a superuser/bypass-RLS connection
/// (see AuditRepository — it uses the same AppDbContext which connects as the postgres
/// superuser configured in ConnectionStrings:DefaultConnection).
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The user who performed the action. Null for system-generated entries.</summary>
    public Guid? UserId { get; init; }

    /// <summary>One of the defined AuditAction constants.</summary>
    public required string Action { get; init; }

    /// <summary>Nullable resource type (RECEIPT, EXPENSE, USER).</summary>
    public string? ResourceType { get; init; }

    /// <summary>Nullable foreign key to the affected resource.</summary>
    public Guid? ResourceId { get; init; }

    /// <summary>JSON snapshot of the resource state before the action.</summary>
    public string? BeforeJson { get; init; }

    /// <summary>JSON snapshot of the resource state after the action.</summary>
    public string? AfterJson { get; init; }

    /// <summary>IPv4 or IPv6 address of the requesting client (max 45 chars).</summary>
    public required string IpAddress { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Named constants for audit action values — no magic strings.</summary>
public static class AuditAction
{
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string ReceiptUpload = "RECEIPT_UPLOAD";
    public const string ExpenseCreate = "EXPENSE_CREATE";
    public const string ExpenseUpdate = "EXPENSE_UPDATE";
    public const string ExpenseDelete = "EXPENSE_DELETE";
    public const string UserInvite = "USER_INVITE";
    public const string RoleChange = "ROLE_CHANGE";
    public const string MfaChange = "MFA_CHANGE";
}

/// <summary>Named constants for resource type values.</summary>
public static class AuditResourceType
{
    public const string Receipt = "RECEIPT";
    public const string Expense = "EXPENSE";
    public const string User = "USER";
}
