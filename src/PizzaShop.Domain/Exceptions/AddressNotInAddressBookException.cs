namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an operation references a <c>CustomerAddress</c> id that is not part of
/// the customer's address book (domain-model.md 6).
/// </summary>
public sealed class AddressNotInAddressBookException : DomainException
{
    public AddressNotInAddressBookException(Guid addressId)
        : base($"Address '{addressId}' is not in the customer's address book.")
    {
    }
}
