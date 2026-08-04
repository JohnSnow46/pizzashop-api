namespace PizzaShop.Domain.Exceptions;

public sealed class TooManyOrderItemsException : DomainException
{
    public TooManyOrderItemsException(int itemsCount)
    : base($"Order has {itemsCount} items, which exceeds the maximum of 50.")
    {
    }
}