namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an order with <c>FulfillmentType.Delivery</c> is created without a
/// delivery address (domain-model.md 5.4 rule 2).
/// </summary>
public sealed class DeliveryAddressRequiredException : DomainException
{
    public DeliveryAddressRequiredException()
        : base("A delivery address is required for delivery orders.")
    {
    }
}
