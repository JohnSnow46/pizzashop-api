namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a pizza-category menu item has no base ingredients
/// (domain-model.md 4, "Pizza musi mieć minimum jeden składnik bazowy").
/// </summary>
public sealed class PizzaWithoutIngredientException : DomainException
{
    public PizzaWithoutIngredientException()
        : base("A pizza must have at least one base ingredient.")
    {
    }
}
