using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto>;
