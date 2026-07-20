using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Creates a staff account — <see cref="UserRole.Employee"/>,
/// <see cref="UserRole.RestaurantAdmin"/> or <see cref="UserRole.SuperAdmin"/>, never
/// <see cref="UserRole.Customer"/> (rejected by shape validation — use
/// <see cref="RegisterCustomerCommand"/> instead). No <see cref="Domain.Customers.Customer"/>
/// profile is created (api-layer.md 2.4/2.5, ADR-0004/0026). Who may create which role is
/// enforced in the handler from <c>ICurrentUser.Role</c> — the endpoint only requires the
/// <c>Admin</c> policy; the finer-grained "RestaurantAdmin can only create Employee" rule
/// depends on the requested target role, which a static <c>[Authorize]</c> attribute can't
/// express (ADR-0017).
/// </summary>
public sealed record RegisterStaffAccountCommand(string Email, string Password, UserRole Role) : ICommand<AuthResultDto>;
