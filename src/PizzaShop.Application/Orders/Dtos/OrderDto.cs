using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Orders.Dtos;

public sealed record OrderDto(
    Guid Id,
    string Number,
    Guid? CustomerId,
    ContactDetailsDto Contact,
    FulfillmentType FulfillmentType,
    AddressDto? DeliveryAddress,
    DateTimeOffset PlacedAt,
    DateTimeOffset? RequestedFulfillmentTime,
    DateTimeOffset? EstimatedReadyAt,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    MoneyDto Subtotal,
    MoneyDto DiscountAmount,
    MoneyDto DeliveryFee,
    MoneyDto Total,
    IReadOnlyList<OrderItemDto> Items);
