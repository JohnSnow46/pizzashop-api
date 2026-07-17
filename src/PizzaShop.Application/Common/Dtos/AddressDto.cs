namespace PizzaShop.Application.Common.Dtos;

/// <summary>
/// DTO mirror of Domain's <see cref="PizzaShop.Domain.ValueObjects.Address"/>.
/// </summary>
public sealed record AddressDto(
    string Street,
    string BuildingNumber,
    string City,
    string PostalCode,
    string? ApartmentNumber = null,
    string? Notes = null);
