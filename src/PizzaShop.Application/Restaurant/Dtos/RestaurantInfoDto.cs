using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Restaurant.Dtos;

/// <summary>
/// Public restaurant info: name, address and opening hours only (no delivery/ordering
/// configuration) — for display contexts like a footer or "about" page.
/// </summary>
public sealed record RestaurantInfoDto(
    string Name,
    AddressDto Address,
    OpeningHoursDto OpeningHours);
