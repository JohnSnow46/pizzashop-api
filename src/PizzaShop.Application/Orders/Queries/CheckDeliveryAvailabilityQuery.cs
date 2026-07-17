using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

/// <summary>
/// Flow step 2 (CLAUDE.md): geocodes <paramref name="Address"/> and checks it against the
/// restaurant's delivery radius, before the customer proceeds to build a cart.
/// </summary>
public sealed record CheckDeliveryAvailabilityQuery(AddressDto Address) : IQuery<DeliveryAvailabilityDto>;
