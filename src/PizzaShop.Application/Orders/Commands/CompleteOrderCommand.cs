using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Marks an order as completed/delivered. Loyalty point accrual (application-layer.md 4.6)
/// is deferred to Iteration 4 and does not happen here yet.
/// </summary>
public sealed record CompleteOrderCommand(Guid OrderId) : ICommand;
