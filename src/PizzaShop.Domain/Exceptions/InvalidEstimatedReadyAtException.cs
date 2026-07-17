namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when <c>EstimatedReadyAt</c> is set outside the allowed status range or before
/// <c>PlacedAt</c> (domain-model.md 5.4 rule 9, ADR-0008).
/// </summary>
public sealed class InvalidEstimatedReadyAtException : DomainException
{
    public InvalidEstimatedReadyAtException(string message)
        : base(message)
    {
    }
}
