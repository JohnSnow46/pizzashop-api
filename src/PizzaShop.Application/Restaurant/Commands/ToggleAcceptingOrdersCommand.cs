using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed record ToggleAcceptingOrdersCommand(bool IsAccepting) : ICommand;
