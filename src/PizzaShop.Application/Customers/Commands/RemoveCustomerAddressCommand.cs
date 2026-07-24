using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Customers.Commands;

/// <summary>
/// Removes an entry from the current customer's address book. Scoped to
/// <see cref="Common.Abstractions.ICurrentUser.CustomerId"/> — the address must belong to the
/// caller's own <c>Customer</c> aggregate.
/// </summary>
public sealed record RemoveCustomerAddressCommand(Guid AddressId) : ICommand;
