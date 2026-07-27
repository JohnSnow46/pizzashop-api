using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Identity.Commands;

public sealed record RequestPasswordResetCommand(string Email) : ICommand;
