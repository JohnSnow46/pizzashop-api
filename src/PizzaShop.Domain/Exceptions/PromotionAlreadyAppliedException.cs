namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to apply a promotion to an order that already has one applied
/// (domain-model.md 5.4 rule 10, ADR-0009) — only one promotion per order is allowed.
/// </summary>
public sealed class PromotionAlreadyAppliedException : DomainException
{
    public PromotionAlreadyAppliedException()
        : base("A promotion has already been applied to this order.")
    {
    }
}
