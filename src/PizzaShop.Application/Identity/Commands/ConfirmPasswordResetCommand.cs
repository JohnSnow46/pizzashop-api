using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Identity.Commands;

public sealed record ConfirmPasswordResetCommand(string Token, string NewPassword) : ICommand;
