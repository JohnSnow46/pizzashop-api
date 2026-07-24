using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Customers.Dtos;

namespace PizzaShop.Application.Customers.Queries;

/// <summary>
/// Returns the current customer's own address book. Scoped to
/// <see cref="Common.Abstractions.ICurrentUser.CustomerId"/> — there is no id parameter, so a
/// request can never read someone else's addresses.
/// </summary>
public sealed record GetCustomerAddressesQuery : IQuery<IReadOnlyList<CustomerAddressDto>>;
