using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;

namespace PizzaShop.Application.Customers.Commands;

/// <summary>
/// Replaces the label and address of an existing entry in the current customer's address
/// book (domain-model.md 6) — e.g. fixing a typo in the street name without having to
/// remove and re-add the entry (which would also lose <c>IsDefault</c> if it was the
/// default). Scoped to <see cref="Common.Abstractions.ICurrentUser.CustomerId"/>.
/// </summary>
public sealed record UpdateCustomerAddressCommand(Guid AddressId, string Label, AddressDto Address) : ICommand<CustomerAddressDto>;
