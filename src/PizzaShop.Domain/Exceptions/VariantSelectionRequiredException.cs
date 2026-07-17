namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a menu item has variants but none was selected when ordering
/// (domain-model.md 4, "wybór wariantu jest wymagany przy zamawianiu").
/// </summary>
public sealed class VariantSelectionRequiredException : DomainException
{
    public VariantSelectionRequiredException(string menuItemName)
        : base($"Menu item '{menuItemName}' requires a variant to be selected.")
    {
    }
}
