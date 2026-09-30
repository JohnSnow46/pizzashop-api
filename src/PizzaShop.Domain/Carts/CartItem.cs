namespace PizzaShop.Domain.Carts;

/// <summary>
/// One line of a <see cref="Cart"/> (ADR-0043). Deliberately holds no price/name snapshot —
/// unlike <c>OrderItem</c>, a cart is a draft, not a transaction; prices are always computed
/// fresh from the current catalog (frontend for display, <c>CreateOrderCommand</c>
/// authoritatively at checkout). Immutable like <c>OrderItem</c> — a quantity change replaces
/// the item in the owning <see cref="Cart"/> rather than mutating a field in place.
/// </summary>
public sealed class CartItem
{
    public Guid Id { get; }
    public Guid MenuItemId { get; }
    public Guid? VariantId { get; }
    public IReadOnlyList<Guid> ExtraIds { get; }
    public int Quantity { get; }
    public string? Notes { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private CartItem()
    {
        ExtraIds = Array.Empty<Guid>();
    }

    private CartItem(Guid id, Guid menuItemId, Guid? variantId, IReadOnlyList<Guid> extraIds, int quantity, string? notes)
    {
        Id = id;
        MenuItemId = menuItemId;
        VariantId = variantId;
        ExtraIds = extraIds;
        Quantity = quantity;
        Notes = notes;
    }

    internal static CartItem Create(
        Guid menuItemId, Guid? variantId, IEnumerable<Guid> extraIds, int quantity, string? notes)
    {
        if (menuItemId == Guid.Empty)
            throw new ArgumentException("Menu item id is required.", nameof(menuItemId));
        if (quantity is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 100.");

        var trimmedNotes = notes?.Trim();
        if (trimmedNotes is { Length: 0 })
            trimmedNotes = null;
        if (trimmedNotes is { Length: > 500 })
            throw new ArgumentException("Notes must not exceed 500 characters.", nameof(notes));

        return new CartItem(Guid.NewGuid(), menuItemId, variantId, DedupeExtraIds(extraIds), quantity, trimmedNotes);
    }

    /// <summary>Same identity key as a new item created with the given selection — used by
    /// <see cref="Cart.AddItem"/> to decide whether to merge quantities (ADR-0043 §B).</summary>
    internal bool HasSameSelection(Guid menuItemId, Guid? variantId, IEnumerable<Guid> extraIds) =>
        MenuItemId == menuItemId && VariantId == variantId && ExtraIds.SequenceEqual(DedupeExtraIds(extraIds));

    internal CartItem WithQuantity(int quantity)
    {
        if (quantity is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 100.");

        return new CartItem(Id, MenuItemId, VariantId, ExtraIds, quantity, Notes);
    }

    private static IReadOnlyList<Guid> DedupeExtraIds(IEnumerable<Guid> extraIds) =>
        extraIds.Distinct().OrderBy(id => id).ToArray();
}
