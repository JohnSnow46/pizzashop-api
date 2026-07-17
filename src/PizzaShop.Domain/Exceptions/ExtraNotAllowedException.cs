namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an extra ingredient requested for an order item does not belong to the
/// menu item's <c>AllowedExtras</c> (domain-model.md 4, "Reguły biznesowe MenuItem").
/// </summary>
public sealed class ExtraNotAllowedException : DomainException
{
    public ExtraNotAllowedException(string ingredientName, string menuItemName)
        : base($"Ingredient '{ingredientName}' is not an allowed extra for menu item '{menuItemName}'.")
    {
    }
}
