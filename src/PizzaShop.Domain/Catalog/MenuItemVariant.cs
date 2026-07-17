using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Catalog;

/// <summary>
/// Size/price variant of a menu item, e.g. "Mała 30cm" (domain-model.md 4.2).
/// </summary>
public class MenuItemVariant
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public bool IsDefault { get; private set; }

    private MenuItemVariant(Guid id, string name, Money price, bool isDefault)
    {
        Id = id;
        Name = name;
        Price = price;
        IsDefault = isDefault;
    }

    public static MenuItemVariant Create(string name, Money price, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(price);

        return new MenuItemVariant(Guid.NewGuid(), name, price, isDefault);
    }

    internal void UpdatePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        Price = price;
    }

    internal void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name;
    }

    internal void MarkDefault() => IsDefault = true;

    internal void UnsetDefault() => IsDefault = false;
}
