using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

/// <summary>
/// The logged-in customer's own order history, newest first, no paging (ADR-0039). Scoped via
/// <c>ICurrentUser.CustomerId</c> in the handler — carries no input.
/// </summary>
public sealed record GetMyOrdersQuery : IQuery<IReadOnlyList<OrderSummaryDto>>;
