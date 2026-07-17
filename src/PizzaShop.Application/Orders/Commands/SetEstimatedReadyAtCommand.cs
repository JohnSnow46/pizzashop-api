using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

public sealed record SetEstimatedReadyAtCommand(Guid OrderId, DateTimeOffset EstimatedReadyAt) : ICommand;
