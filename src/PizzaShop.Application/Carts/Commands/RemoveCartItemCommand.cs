using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Commands;

public sealed record RemoveCartItemCommand(Guid CartItemId) : ICommand;
