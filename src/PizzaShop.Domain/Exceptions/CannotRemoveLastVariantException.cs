namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to remove the only remaining variant of a menu item — once a
/// menu item is configured with variants, the list cannot become empty through removal
/// (domain-model.md 4.4, ADR-0016).
/// </summary>
public sealed class CannotRemoveLastVariantException : DomainException
{
    public CannotRemoveLastVariantException(string menuItemName)
        : base($"Cannot remove the last variant of menu item '{menuItemName}'.")
    {
    }
}
