using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Commands;

public sealed record UpdateCartItemQuantityCommand(Guid CartItemId, int Quantity) : ICommand;
