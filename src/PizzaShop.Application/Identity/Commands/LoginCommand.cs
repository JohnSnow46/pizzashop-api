using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Authenticates an existing account and issues a JWT (api-layer.md 2.4/2.7, ADR-0026).
/// Anonymous — no <c>ICurrentUser</c> role is involved.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResultDto>;
