using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Places a new order (application-layer.md 4.3.1). <c>CustomerId</c> is deliberately not a
/// field here — the handler reads it from <see cref="Common.Abstractions.ICurrentUser"/>
/// (null means a guest order), so a client request body can never spoof it.
/// Payment-gateway initialization (Iteration 3, ADR-0013) is wired in:
/// <c>PaymentMethod.Online</c> orders return a <c>PaymentRedirectUrl</c> via
/// <c>CreateOrderResultDto</c>. <see cref="PromotionCode"/> (step 6) and
/// <see cref="PointsToRedeem"/> (step 7, only applied for a logged-in customer) are optional —
/// omitting them places the order without a discount/redemption, as before Iteration 4.
/// </summary>
public sealed record CreateOrderCommand(
    ContactDetailsDto Contact,
    FulfillmentType FulfillmentType,
    AddressDto? DeliveryAddress,
    IReadOnlyList<CreateOrderItemDto> Items,
    DateTimeOffset? RequestedFulfillmentTime,
    PaymentMethod PaymentMethod,
    string? PromotionCode = null,
    int? PointsToRedeem = null) : ICommand<CreateOrderResultDto>;
