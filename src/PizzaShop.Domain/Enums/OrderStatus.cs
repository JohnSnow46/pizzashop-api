namespace PizzaShop.Domain.Enums;

/// <summary>
/// Fulfillment lifecycle of an order. See docs/domain-model.md section 5.3 and ADR-0007
/// for the full state graph and its coupling (or lack thereof) with <see cref="PaymentStatus"/>.
/// </summary>
public enum OrderStatus
{
    PendingAcceptance,
    Accepted,
    InPreparation,
    Ready,
    OutForDelivery,
    Completed,
    Rejected,
    Cancelled,
}
