namespace ExpenseTracker.Shared.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string resource, object id)
    : DomainException($"{resource} with id '{id}' was not found.");

public sealed class UnauthorizedException(string message = "Unauthorized.")
    : DomainException(message);

public sealed class ForbiddenException(string message = "Access denied.")
    : DomainException(message);

public sealed class ConflictException(string message)
    : DomainException(message);

public sealed class ValidationException(string message)
    : DomainException(message);
