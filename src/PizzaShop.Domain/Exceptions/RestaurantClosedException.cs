namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an order (ASAP or scheduled) is requested at a time the restaurant is
/// not open or not currently accepting orders (domain-model.md 5.4 rule 6).
/// </summary>
public sealed class RestaurantClosedException : DomainException
{
    public RestaurantClosedException(DateTimeOffset requestedAt)
        : base($"Restaurant is closed at {requestedAt:O}.")
    {
    }
}
