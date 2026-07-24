using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a loyalty points redemption's discount value would exceed the amount still
/// payable on the order (domain-model.md 5.4 rule 10, ADR-0040) — a purely mathematical
/// guard, distinct from <see cref="InsufficientLoyaltyPointsException"/> (which checks the
/// customer's balance, not the order's remaining value).
/// </summary>
public sealed class LoyaltyRedemptionExceedsOrderValueException : DomainException
{
    public LoyaltyRedemptionExceedsOrderValueException(int points, Money discountAmount, Money maxDiscount)
        : base(
            $"Cannot redeem {points} points for a discount of {discountAmount}; " +
            $"the maximum discount for this order is {maxDiscount}.")
    {
    }
}
