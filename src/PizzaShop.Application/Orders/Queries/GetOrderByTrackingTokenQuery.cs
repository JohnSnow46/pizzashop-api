using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

/// <summary>
/// Guest order tracking (application-layer.md 4.3, flow step 3). No JWT involved —
/// possession of the unguessable <see cref="GuestTrackingToken"/> is the access control.
/// </summary>
public sealed record GetOrderByTrackingTokenQuery(Guid GuestTrackingToken) : IQuery<OrderDto>;
