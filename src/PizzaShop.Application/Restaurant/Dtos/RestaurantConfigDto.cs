using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Restaurant.Dtos;

/// <summary>
/// Public-facing restaurant configuration: opening hours, delivery area, ordering
/// thresholds (application-layer.md 4.2).
/// </summary>
public sealed record RestaurantConfigDto(
    Guid Id,
    string Name,
    AddressDto Address,
    GeoCoordinateDto Location,
    double DeliveryRadiusKm,
    string TimeZoneId,
    OpeningHoursDto OpeningHours,
    string ContactPhone,
    bool IsAcceptingOrders,
    MoneyDto? MinimumOrderValue,
    MoneyDto? FreeDeliveryThreshold,
    MoneyDto DeliveryFee);
