namespace ExpenseTracker.Audit.Middleware;

/// <summary>
/// Opt-in marker attribute for Minimal API endpoints.
/// When present the <see cref="AuditMiddleware"/> captures the event and writes
/// an audit log entry with the declared action name and resource type.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuditedAttribute(string action, string? resourceType = null) : Attribute
{
    public string Action { get; } = action;
    public string? ResourceType { get; } = resourceType;
}
