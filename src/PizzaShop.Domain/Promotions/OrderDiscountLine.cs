using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Promotions;

/// <summary>
/// One line item's contribution to a discount calculation (domain-model.md 8.2, ADR-0034) —
/// a transient projection of <c>OrderItem</c> built by Application, not persisted and not
/// referencing the <c>Order</c> aggregate (keeps <see cref="Promotion"/> decoupled from
/// <c>Order</c>, ADR-0011).
/// </summary>
public sealed record OrderDiscountLine(Guid MenuItemId, Money UnitPrice, int Quantity)
{
    public Guid MenuItemId { get; init; } = MenuItemId == Guid.Empty
        ? throw new ArgumentException("Menu item id is required.", nameof(MenuItemId))
        : MenuItemId;

    public Money UnitPrice { get; init; } = UnitPrice ?? throw new ArgumentNullException(nameof(UnitPrice));

    public int Quantity { get; init; } = Quantity < 1
        ? throw new ArgumentOutOfRangeException(nameof(Quantity), "Quantity must be at least 1.")
        : Quantity;
}
