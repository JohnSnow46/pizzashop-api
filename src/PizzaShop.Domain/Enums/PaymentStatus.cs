namespace PizzaShop.Domain.Enums;

/// <summary>
/// Payment lifecycle of an order, independent from <see cref="OrderStatus"/> (ADR-0007).
/// </summary>
public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Refunded,
    Failed,
}
