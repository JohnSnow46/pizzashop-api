namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to calculate or apply a discount for a promotion that does not
/// currently qualify (inactive, outside its validity window, below the minimum order
/// value, code mismatch, or usage limit reached) — domain-model.md 8, "Reguły (zarys)".
/// Not explicitly named in domain-model.md section 9; added alongside
/// <c>InvalidPaymentStatusTransitionException</c> because <c>Promotion</c> needs the same
/// kind of guard as the other aggregates.
/// </summary>
public sealed class PromotionNotApplicableException : DomainException
{
    public PromotionNotApplicableException(string message)
        : base(message)
    {
    }
}
