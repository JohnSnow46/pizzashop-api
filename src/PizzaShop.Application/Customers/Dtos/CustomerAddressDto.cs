using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Customers.Dtos;

/// <summary>
/// DTO mirror of a <see cref="PizzaShop.Domain.Customers.CustomerAddress"/> address-book entry.
/// </summary>
public sealed record CustomerAddressDto(Guid Id, string Label, AddressDto Address, bool IsDefault);
