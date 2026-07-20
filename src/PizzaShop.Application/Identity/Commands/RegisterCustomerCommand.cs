using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Registers a new customer account (api-layer.md 2.4/2.5, ADR-0026). Creates
/// <see cref="UserAccount"/> + <see cref="Domain.Customers.Customer"/> +
/// <see cref="Domain.Loyalty.LoyaltyAccount"/> atomically and returns a JWT (auto-login).
/// Anonymous — no <c>ICurrentUser</c> role is involved.
/// </summary>
public sealed record RegisterCustomerCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber) : ICommand<AuthResultDto>;
