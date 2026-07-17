namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity looked up by id/token does not exist. Application-level —
/// mapped to HTTP 404 in the Api middleware (application-layer.md 5).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}
