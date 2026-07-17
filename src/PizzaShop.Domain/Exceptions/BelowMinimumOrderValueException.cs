using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when the order subtotal is below the restaurant's configured minimum order
/// value (domain-model.md 5.4 rule 4).
/// </summary>
public sealed class BelowMinimumOrderValueException : DomainException
{
    public BelowMinimumOrderValueException(Money subtotal, Money minimumOrderValue)
        : base($"Order subtotal {subtotal.Amount} {subtotal.Currency} is below the minimum order value of {minimumOrderValue.Amount} {minimumOrderValue.Currency}.")
    {
    }
}
