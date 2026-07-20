using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Catalog;

/// <summary>
/// Catalog item (pizza, drink, side, dessert, sauce). Pizza-specific rules (base
/// ingredients, allowed extras) are enforced conditionally on <see cref="Category"/>
/// (domain-model.md 4, section 10 "MenuItem + Category zamiast hierarchii klas").
/// </summary>
public class MenuItem
{
    private readonly List<MenuItemVariant> _variants = new();
    private readonly List<Ingredient> _baseIngredients = new();
    private readonly List<Ingredient> _allowedExtras = new();

    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public MenuCategory Category { get; }
    public Money BasePrice { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? ImageUrl { get; private set; }

    public IReadOnlyCollection<MenuItemVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<Ingredient> BaseIngredients => _baseIngredients.AsReadOnly();
    public IReadOnlyCollection<Ingredient> AllowedExtras => _allowedExtras.AsReadOnly();

    public MenuItemVariant? DefaultVariant => _variants.FirstOrDefault(v => v.IsDefault);

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private MenuItem()
    {
    }

    private MenuItem(Guid id, string name, MenuCategory category, Money basePrice)
    {
        Id = id;
        Name = name;
        Category = category;
        BasePrice = basePrice;
        IsAvailable = true;
    }

    public static MenuItem Create(
        string name,
        MenuCategory category,
        Money basePrice,
        string? description = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(basePrice);

        return new MenuItem(Guid.NewGuid(), name, category, basePrice)
        {
            Description = description,
            ImageUrl = imageUrl,
        };
    }

    public void AddBaseIngredient(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        if (_baseIngredients.Any(i => i.Id == ingredient.Id))
            return;

        _baseIngredients.Add(ingredient);
    }

    public void RemoveBaseIngredient(Guid ingredientId) =>
        _baseIngredients.RemoveAll(i => i.Id == ingredientId);

    public void AllowExtra(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        if (_allowedExtras.Any(i => i.Id == ingredient.Id))
            return;

        _allowedExtras.Add(ingredient);
    }

    public void DisallowExtra(Guid ingredientId) =>
        _allowedExtras.RemoveAll(i => i.Id == ingredientId);

    public void AddVariant(MenuItemVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);

        if (variant.IsDefault)
        {
            foreach (var existing in _variants)
                existing.UnsetDefault();
        }

        _variants.Add(variant);
    }

    public void MarkAvailable() => IsAvailable = true;

    public void MarkUnavailable() => IsAvailable = false;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name;
    }

    public void UpdateBasePrice(Money basePrice)
    {
        ArgumentNullException.ThrowIfNull(basePrice);
        BasePrice = basePrice;
    }

    /// <summary>
    /// Replaces description/image in one go (PUT semantics — <c>null</c> clears the value).
    /// Both fields are optional and carry no invariants, so a single method covers both
    /// instead of two separate setters (domain-model.md 4.4).
    /// </summary>
    public void UpdateDetails(string? description, string? imageUrl)
    {
        Description = description;
        ImageUrl = imageUrl;
    }

    /// <summary>
    /// Marks the given variant as the default one, unsetting the previous default.
    /// Idempotent when the variant is already the default (domain-model.md 4.4, ADR-0016).
    /// </summary>
    public void SetDefaultVariant(Guid variantId)
    {
        var variant = FindVariant(variantId);

        if (variant.IsDefault)
            return;

        foreach (var existing in _variants.Where(v => v.IsDefault))
            existing.UnsetDefault();

        variant.MarkDefault();
    }

    /// <summary>
    /// Removes a variant, guarding the invariants: the variant must exist, it cannot be the
    /// last remaining variant, and the default variant cannot be removed while other
    /// variants exist without first calling <see cref="SetDefaultVariant"/>
    /// (domain-model.md 4.4, ADR-0016).
    /// </summary>
    public void RemoveVariant(Guid variantId)
    {
        var variant = FindVariant(variantId);

        if (_variants.Count == 1)
            throw new CannotRemoveLastVariantException(Name);

        if (variant.IsDefault)
            throw new InvalidVariantConfigurationException(
                $"Cannot remove the default variant of menu item '{Name}' while other variants exist. Call SetDefaultVariant first.");

        _variants.Remove(variant);
    }

    /// <summary>
    /// Renames an existing variant through the aggregate root (domain-model.md 4.4).
    /// </summary>
    public void RenameVariant(Guid variantId, string name) => FindVariant(variantId).Rename(name);

    /// <summary>
    /// Updates the price of an existing variant through the aggregate root
    /// (domain-model.md 4.4).
    /// </summary>
    public void UpdateVariantPrice(Guid variantId, Money price) => FindVariant(variantId).UpdatePrice(price);

    private MenuItemVariant FindVariant(Guid variantId) =>
        _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new InvalidVariantConfigurationException(
                $"Variant '{variantId}' does not belong to menu item '{Name}'.");

    /// <summary>
    /// Validates catalog-level invariants: pizzas need at least one base ingredient,
    /// and — when variants exist — exactly one must be the default
    /// (domain-model.md 4, "Reguły biznesowe MenuItem").
    /// </summary>
    public void EnsureValidCatalogConfiguration()
    {
        if (Category == MenuCategory.Pizza && _baseIngredients.Count == 0)
            throw new PizzaWithoutIngredientException();

        if (_variants.Count > 0 && _variants.Count(v => v.IsDefault) != 1)
            throw new InvalidVariantConfigurationException(
                $"Menu item '{Name}' must have exactly one default variant when variants are defined.");
    }

    public void EnsureExtraAllowed(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        if (_allowedExtras.All(i => i.Id != ingredient.Id))
            throw new ExtraNotAllowedException(ingredient.Name, Name);
    }

    /// <summary>
    /// Resolves the unit price (and, if applicable, the selected variant) to use when
    /// adding this item to an order. Enforces availability, catalog configuration, and
    /// the "variant required if variants exist" rule (domain-model.md 4).
    /// </summary>
    public (Money UnitPrice, Guid? VariantId, string? VariantName) ResolvePrice(Guid? variantId)
    {
        if (!IsAvailable)
            throw new MenuItemUnavailableException(Name);

        EnsureValidCatalogConfiguration();

        if (_variants.Count == 0)
        {
            if (variantId.HasValue)
                throw new InvalidVariantConfigurationException($"Menu item '{Name}' has no variants to select.");

            return (BasePrice, null, null);
        }

        if (!variantId.HasValue)
            throw new VariantSelectionRequiredException(Name);

        var variant = _variants.FirstOrDefault(v => v.Id == variantId.Value)
            ?? throw new InvalidVariantConfigurationException(
                $"Variant '{variantId}' does not belong to menu item '{Name}'.");

        return (variant.Price, variant.Id, variant.Name);
    }
}
