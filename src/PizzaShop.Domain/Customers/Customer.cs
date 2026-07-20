using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Customers;

/// <summary>
/// Purchasing profile of a registered customer (domain-model.md 6, ADR-0005). Identity
/// (login/password/role) lives outside Domain as <c>UserAccount</c>; here we only keep
/// the <see cref="UserAccountId"/> reference.
/// </summary>
public class Customer
{
    private readonly List<CustomerAddress> _addressBook = new();

    public Guid Id { get; }
    public Guid UserAccountId { get; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<CustomerAddress> AddressBook => _addressBook.AsReadOnly();

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Customer()
    {
    }

    private Customer(
        Guid id,
        Guid userAccountId,
        string fullName,
        string email,
        string? phoneNumber,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserAccountId = userAccountId;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        CreatedAt = createdAt;
    }

    public static Customer Create(
        Guid userAccountId,
        string fullName,
        string email,
        DateTimeOffset createdAt,
        string? phoneNumber = null)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("User account id is required.", nameof(userAccountId));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        // The Customer <-> LoyaltyAccount 1:1 link is one-directional (ADR-0029):
        // LoyaltyAccount carries the FK (CustomerId), Customer carries none. This avoids the
        // chicken-and-egg id coordination between the two aggregates.
        return new Customer(Guid.NewGuid(), userAccountId, fullName, email, phoneNumber, createdAt);
    }

    public CustomerAddress AddAddress(string label, DeliveryAddress deliveryAddress, bool isDefault = false)
    {
        if (isDefault)
        {
            foreach (var existing in _addressBook)
                existing.UnsetDefault();
        }

        var entry = CustomerAddress.Create(label, deliveryAddress, isDefault);
        _addressBook.Add(entry);
        return entry;
    }

    public void RemoveAddress(Guid addressId)
    {
        _addressBook.RemoveAll(a => a.Id == addressId);
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var target = _addressBook.FirstOrDefault(a => a.Id == addressId)
            ?? throw new AddressNotInAddressBookException(addressId);

        foreach (var entry in _addressBook)
            entry.UnsetDefault();

        target.SetAsDefault();
    }

    public void UpdateContactDetails(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName;
        PhoneNumber = phoneNumber;
    }
}
