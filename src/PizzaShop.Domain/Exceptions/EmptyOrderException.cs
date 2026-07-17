namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to create or persist an order without any items
/// (domain-model.md 5.4 rule 1).
/// </summary>
public sealed class EmptyOrderException : DomainException
{
    public EmptyOrderException()
        : base("An order must contain at least one item.")
    {
    }
}
