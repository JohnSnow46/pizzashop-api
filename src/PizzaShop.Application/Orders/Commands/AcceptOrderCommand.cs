using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Accepts a pending order into the kitchen (application-layer.md 4.3.2). Optionally sets
/// <see cref="EstimatedReadyAt"/> in the same step — staff may instead call
/// <c>SetEstimatedReadyAtCommand</c> later.
/// </summary>
public sealed record AcceptOrderCommand(Guid OrderId, DateTimeOffset? EstimatedReadyAt = null) : ICommand;
