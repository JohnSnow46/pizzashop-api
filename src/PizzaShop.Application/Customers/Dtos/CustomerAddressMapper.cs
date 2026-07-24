using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Customers.Dtos;

internal static class CustomerAddressMapper
{
    public static CustomerAddressDto ToDto(CustomerAddress entry) =>
        new(entry.Id, entry.Label, ToDto(entry.DeliveryAddress.Address), entry.IsDefault);

    private static AddressDto ToDto(Address address) =>
        new(address.Street, address.BuildingNumber, address.City, address.PostalCode, address.ApartmentNumber, address.Notes);
}
