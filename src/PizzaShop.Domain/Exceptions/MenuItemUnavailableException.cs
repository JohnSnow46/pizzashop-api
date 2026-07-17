namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to order a menu item that is currently unavailable
/// (domain-model.md 4, "Nie można zamówić pozycji z IsAvailable == false").
/// </summary>
public sealed class MenuItemUnavailableException : DomainException
{
    public MenuItemUnavailableException(string menuItemName)
        : base($"Menu item '{menuItemName}' is not currently available.")
    {
    }
}
