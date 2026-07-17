using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

/// <summary>
/// Incoming order queue for staff (application-layer.md 4.3, <c>Employee+</c>).
/// </summary>
public sealed record GetOrderQueueQuery : IQuery<IReadOnlyList<OrderDto>>;
