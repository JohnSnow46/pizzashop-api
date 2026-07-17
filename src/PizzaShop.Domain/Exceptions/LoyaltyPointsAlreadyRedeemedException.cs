namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to redeem loyalty points on an order that has already had
/// points redeemed against it (domain-model.md 5.4 rule 10, ADR-0009) — redemption can
/// only happen once per order.
/// </summary>
public sealed class LoyaltyPointsAlreadyRedeemedException : DomainException
{
    public LoyaltyPointsAlreadyRedeemedException()
        : base("Loyalty points have already been redeemed on this order.")
    {
    }
}
