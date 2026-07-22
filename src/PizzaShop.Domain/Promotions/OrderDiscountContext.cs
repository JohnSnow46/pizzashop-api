using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Promotions;

/// <summary>
/// Everything <see cref="Promotion.CalculateDiscount"/> needs to know about the order being
/// discounted (domain-model.md 8.2, ADR-0034) — a transient VO built by Application from
/// <c>order.Subtotal</c>/<c>order.DeliveryFee</c>/<c>order.Items</c>. Deliberately has no
/// reference to the <c>Order</c>/<c>OrderItem</c> entities: <see cref="Promotion"/> stays
/// decoupled from the <c>Order</c> aggregate (ADR-0011, aggregate boundaries).
/// </summary>
public sealed record OrderDiscountContext(
    Money Subtotal,
    Money DeliveryFee,
    DateTimeOffset When,
    string? SuppliedCode,
    IReadOnlyList<OrderDiscountLine> Lines)
{
    public Money Subtotal { get; init; } = Subtotal ?? throw new ArgumentNullException(nameof(Subtotal));

    public Money DeliveryFee { get; init; } = DeliveryFee ?? throw new ArgumentNullException(nameof(DeliveryFee));

    public IReadOnlyList<OrderDiscountLine> Lines { get; init; } = Lines ?? throw new ArgumentNullException(nameof(Lines));
}
