namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Snapshot of an extra ingredient added to an <c>OrderItem</c>: name and price copied
/// from the catalog at order time (domain-model.md 5.2).
/// </summary>
public sealed class OrderItemExtra : IEquatable<OrderItemExtra>
{
    public Guid IngredientId { get; }
    public string Name { get; }
    public Money Price { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private OrderItemExtra()
    {
    }

    public OrderItemExtra(Guid ingredientId, string name, Money price)
    {
        if (ingredientId == Guid.Empty)
            throw new ArgumentException("Ingredient id is required.", nameof(ingredientId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(price);

        IngredientId = ingredientId;
        Name = name;
        Price = price;
    }

    public bool Equals(OrderItemExtra? other) =>
        other is not null
        && IngredientId == other.IngredientId
        && Name == other.Name
        && Price == other.Price;

    public override bool Equals(object? obj) => Equals(obj as OrderItemExtra);

    public override int GetHashCode() => HashCode.Combine(IngredientId, Name, Price);

    public static bool operator ==(OrderItemExtra? left, OrderItemExtra? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(OrderItemExtra? left, OrderItemExtra? right) => !(left == right);
}
