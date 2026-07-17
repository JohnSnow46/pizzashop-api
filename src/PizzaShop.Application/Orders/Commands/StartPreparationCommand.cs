using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

public sealed record StartPreparationCommand(Guid OrderId) : ICommand;
