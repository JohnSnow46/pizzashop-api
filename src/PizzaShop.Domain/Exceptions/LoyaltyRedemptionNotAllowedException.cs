namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to redeem loyalty points on a guest order
/// (no <c>CustomerId</c>) — domain-model.md 5.4 rule 10, ADR-0005.
/// </summary>
public sealed class LoyaltyRedemptionNotAllowedException : DomainException
{
    public LoyaltyRedemptionNotAllowedException()
        : base("Loyalty points can only be redeemed on orders placed by a registered customer.")
    {
    }
}
