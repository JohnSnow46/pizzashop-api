using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Orders;

/// <summary>
/// Order line item. Copies the menu item/variant name and price as a snapshot at order
/// time, so historical orders are unaffected by later catalog changes
/// (domain-model.md 5.2, "Zasada snapshotów").
/// </summary>
public class OrderItem
{
    private readonly List<OrderItemExtra> _extras = new();

    public Guid Id { get; }
    public Guid MenuItemId { get; }
    public string MenuItemName { get; }
    public Guid? VariantId { get; }
    public string? VariantName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; }
    public string? Notes { get; }

    public IReadOnlyCollection<OrderItemExtra> Extras => _extras.AsReadOnly();

    public Money LineTotal { get; }

    private OrderItem(
        Guid id,
        Guid menuItemId,
        string menuItemName,
        Guid? variantId,
        string? variantName,
        Money unitPrice,
        int quantity,
        IEnumerable<OrderItemExtra> extras,
        string? notes)
    {
        Id = id;
        MenuItemId = menuItemId;
        MenuItemName = menuItemName;
        VariantId = variantId;
        VariantName = variantName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        _extras.AddRange(extras);
        Notes = notes;
        LineTotal = CalculateLineTotal();
    }

    public static OrderItem Create(
        Guid menuItemId,
        string menuItemName,
        Money unitPrice,
        int quantity,
        Guid? variantId = null,
        string? variantName = null,
        IEnumerable<OrderItemExtra>? extras = null,
        string? notes = null)
    {
        if (menuItemId == Guid.Empty)
            throw new ArgumentException("Menu item id is required.", nameof(menuItemId));
        if (string.IsNullOrWhiteSpace(menuItemName))
            throw new ArgumentException("Menu item name is required.", nameof(menuItemName));
        ArgumentNullException.ThrowIfNull(unitPrice);
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");

        return new OrderItem(
            Guid.NewGuid(),
            menuItemId,
            menuItemName,
            variantId,
            variantName,
            unitPrice,
            quantity,
            extras ?? Enumerable.Empty<OrderItemExtra>(),
            notes);
    }

    private Money CalculateLineTotal()
    {
        var extrasTotal = _extras.Aggregate(Money.Zero(UnitPrice.Currency), (sum, extra) => sum.Add(extra.Price));
        return UnitPrice.Add(extrasTotal).Multiply(Quantity);
    }
}
