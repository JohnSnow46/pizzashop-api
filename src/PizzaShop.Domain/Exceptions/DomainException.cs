namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Base type for all domain rule violations. Concrete exceptions are mapped to HTTP
/// status codes by middleware in the Api layer (see CLAUDE.md).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}
