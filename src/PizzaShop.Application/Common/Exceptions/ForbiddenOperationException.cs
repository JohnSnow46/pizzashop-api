namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown when an operation is legal in general, but the current caller does not have
/// permission to perform it in this state/context — a decision that depends on role or
/// context Domain deliberately does not know about (ADR-0004/ADR-0005). Application-level,
/// mapped to HTTP 403 in the Api middleware (ADR-0017, application-layer.md 5). The message
/// is safe to return to the client.
/// </summary>
public sealed class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException(string message) : base(message)
    {
    }
}
