using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Orders.Dtos;

/// <summary>
/// Result of <c>CheckDeliveryAvailabilityQuery</c> (application-layer.md 4.3, flow step 2).
/// <see cref="DeliveryFee"/> is the restaurant's standard fee — the cart subtotal (and thus
/// any free-delivery threshold) is not yet known at this point in the flow. Both
/// <see cref="DistanceKm"/> and <see cref="DeliveryFee"/> are <c>null</c> when
/// <see cref="IsAvailable"/> is <c>false</c>.
/// </summary>
public sealed record DeliveryAvailabilityDto(bool IsAvailable, double? DistanceKm, MoneyDto? DeliveryFee);
