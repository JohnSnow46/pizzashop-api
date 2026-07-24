using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;

namespace PizzaShop.Application.Customers.Commands;

/// <summary>
/// Adds a new entry to the current customer's address book (domain-model.md 6). Scoped to
/// <see cref="Common.Abstractions.ICurrentUser.CustomerId"/> — there is no customer id
/// parameter, so a request can never write to someone else's address book.
/// </summary>
public sealed record AddCustomerAddressCommand(string Label, AddressDto Address, bool IsDefault) : ICommand<CustomerAddressDto>;
