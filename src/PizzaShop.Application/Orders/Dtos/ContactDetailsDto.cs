namespace PizzaShop.Application.Orders.Dtos;

/// <summary>
/// DTO mirror of Domain's <see cref="PizzaShop.Domain.ValueObjects.ContactDetails"/>.
/// Order-specific — not shared across modules, unlike <c>Common/Dtos</c>.
/// </summary>
public sealed record ContactDetailsDto(string FullName, string PhoneNumber, string? Email = null);
