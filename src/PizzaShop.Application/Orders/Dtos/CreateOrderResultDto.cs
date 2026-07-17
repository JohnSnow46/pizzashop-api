namespace PizzaShop.Application.Orders.Dtos;

/// <summary>
/// Result of <c>CreateOrderCommand</c> (application-layer.md 4.3.1, step 10).
/// <see cref="GuestTrackingToken"/> is set only for guest orders (no <c>CustomerId</c>).
/// <see cref="PaymentRedirectUrl"/> is set only for <see cref="Domain.Enums.PaymentMethod.Online"/>
/// orders (ADR-0013, application-layer.md 4.3.1 step 8) — <c>null</c> for <c>OnPickup</c>.
/// </summary>
public sealed record CreateOrderResultDto(Guid OrderId, string Number, Guid? GuestTrackingToken, string? PaymentRedirectUrl);
