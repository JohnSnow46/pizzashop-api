namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to redeem or spend more loyalty points than the current
/// account balance (domain-model.md 5.4 rule 10, 7.2, ADR-0009).
/// </summary>
public sealed class InsufficientLoyaltyPointsException : DomainException
{
    public InsufficientLoyaltyPointsException(int requestedPoints, int availableBalance)
        : base($"Cannot redeem {requestedPoints} points; available balance is {availableBalance}.")
    {
    }
}
