namespace ExpenseTracker.Audit.Models;

/// <summary>Query parameters for GET /audit.</summary>
public sealed record AuditLogQuery(
    Guid? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Action,
    int Page = 1,
    int PageSize = 50);

/// <summary>Single item in the audit log response.</summary>
public sealed record AuditLogItem(
    Guid Id,
    Guid? UserId,
    string Action,
    string? ResourceType,
    Guid? ResourceId,
    string? BeforeJson,
    string? AfterJson,
    string IpAddress,
    DateTimeOffset CreatedAt);

/// <summary>Paginated response for GET /audit.</summary>
public sealed record AuditLogPagedResponse(
    IReadOnlyList<AuditLogItem> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>Internal request object used by AuditService to write a log entry.</summary>
public sealed record WriteAuditLogRequest(
    Guid? UserId,
    string Action,
    string? ResourceType = null,
    Guid? ResourceId = null,
    string? BeforeJson = null,
    string? AfterJson = null,
    string IpAddress = "");
