using PizzaShop.Domain.Enums;

namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an order fulfillment status transition does not follow the allowed
/// graph (domain-model.md 5.3 / 5.4 rule 7).
/// </summary>
public sealed class InvalidOrderStatusTransitionException : DomainException
{
    public InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
        : base($"Cannot transition order status from '{from}' to '{to}'.")
    {
    }
}
