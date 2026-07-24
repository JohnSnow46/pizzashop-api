using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Orders.Dtos;

/// <summary>
/// Row shown in the customer's own order history (<c>GET /api/orders/mine</c>, ADR-0039) —
/// deliberately slimmer than <see cref="OrderDto"/>; the order detail is fetched separately
/// via <c>GET /api/orders/{id}</c>.
/// </summary>
public sealed record OrderSummaryDto(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    OrderStatus Status,
    FulfillmentType FulfillmentType,
    PaymentStatus PaymentStatus,
    MoneyDto Total,
    int ItemsCount);
