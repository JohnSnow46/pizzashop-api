namespace PizzaShop.Domain.Carts;

/// <summary>
/// A registered customer's persistent, server-side cart (ADR-0043) — one per
/// <see cref="CustomerId"/>. Guests keep using the existing client-side-only cart; this
/// aggregate never applies to them. Holds only reference ids, never price/name snapshots
/// (see <see cref="CartItem"/>).
/// </summary>
public sealed class Cart
{
    private readonly List<CartItem> _items = new();

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Cart()
    {
    }

    private Cart(Guid id, Guid customerId, DateTimeOffset updatedAt)
    {
        Id = id;
        CustomerId = customerId;
        UpdatedAt = updatedAt;
    }

    public static Cart Create(Guid customerId, DateTimeOffset updatedAt)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        return new Cart(Guid.NewGuid(), customerId, updatedAt);
    }

    /// <summary>Adds a new line, or — if a line with the same (menuItemId, variantId,
    /// extraIds) already exists (ADR-0043 §B) — increases that line's quantity instead of
    /// creating a duplicate.</summary>
    public void AddItem(
        Guid menuItemId, Guid? variantId, IEnumerable<Guid> extraIds, int quantity, string? notes, DateTimeOffset updatedAt)
    {
        var extraIdList = extraIds as IReadOnlyList<Guid> ?? extraIds.ToArray();
        var existing = _items.FirstOrDefault(i => i.HasSameSelection(menuItemId, variantId, extraIdList));

        if (existing is null)
        {
            _items.Add(CartItem.Create(menuItemId, variantId, extraIdList, quantity, notes));
        }
        else
        {
            var mergedQuantity = existing.Quantity + quantity;
            if (mergedQuantity > 100)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Combined quantity must not exceed 100.");

            _items.Remove(existing);
            _items.Add(existing.WithQuantity(mergedQuantity));
        }

        UpdatedAt = updatedAt;
    }

    public void UpdateItemQuantity(Guid cartItemId, int quantity, DateTimeOffset updatedAt)
    {
        var existing = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new ArgumentException("Cart item not found.", nameof(cartItemId));

        _items.Remove(existing);
        _items.Add(existing.WithQuantity(quantity));
        UpdatedAt = updatedAt;
    }

    public void RemoveItem(Guid cartItemId, DateTimeOffset updatedAt)
    {
        var existing = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new ArgumentException("Cart item not found.", nameof(cartItemId));

        _items.Remove(existing);
        UpdatedAt = updatedAt;
    }

    public void Clear(DateTimeOffset updatedAt)
    {
        _items.Clear();
        UpdatedAt = updatedAt;
    }
}
