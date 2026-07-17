namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a menu item's variant configuration is invalid — e.g. more than one
/// default variant, or a requested variant that does not belong to the item
/// (domain-model.md 4.2, "dokładnie jeden IsDefault == true").
/// </summary>
public sealed class InvalidVariantConfigurationException : DomainException
{
    public InvalidVariantConfigurationException(string message)
        : base(message)
    {
    }
}
