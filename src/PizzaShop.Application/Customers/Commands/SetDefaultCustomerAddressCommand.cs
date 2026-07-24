using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Customers.Commands;

/// <summary>
/// Marks an existing address-book entry as the current customer's default. Scoped to
/// <see cref="Common.Abstractions.ICurrentUser.CustomerId"/>.
/// </summary>
public sealed record SetDefaultCustomerAddressCommand(Guid AddressId) : ICommand;
