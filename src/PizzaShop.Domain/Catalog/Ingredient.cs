using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Catalog;

/// <summary>
/// Dictionary entity for ingredients used as pizza base components or extras
/// (domain-model.md 4.3).
/// </summary>
public class Ingredient
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Money ExtraPrice { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? Category { get; private set; }

    private Ingredient(Guid id, string name, Money extraPrice, bool isAvailable, string? category)
    {
        Id = id;
        Name = name;
        ExtraPrice = extraPrice;
        IsAvailable = isAvailable;
        Category = category;
    }

    public static Ingredient Create(string name, Money extraPrice, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(extraPrice);

        return new Ingredient(Guid.NewGuid(), name, extraPrice, isAvailable: true, category);
    }

    public void MarkAvailable() => IsAvailable = true;

    public void MarkUnavailable() => IsAvailable = false;

    public void UpdatePrice(Money extraPrice)
    {
        ArgumentNullException.ThrowIfNull(extraPrice);
        ExtraPrice = extraPrice;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name;
    }
}
