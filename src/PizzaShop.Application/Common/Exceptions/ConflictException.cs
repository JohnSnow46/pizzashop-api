namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown for a state conflict that is illegal for every caller, but is detected in
/// Application because the operation does not correspond to any aggregate state transition
/// (so there is no Domain method to call, and modelling it as a <c>DomainException</c> would
/// pull provider/gateway vocabulary into Domain — ADR-0002, ADR-0018). Application-level,
/// mapped to HTTP 409 in the Api middleware (ADR-0017/ADR-0018, application-layer.md 5).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
