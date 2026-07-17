using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

public sealed record MarkReadyCommand(Guid OrderId) : ICommand;
