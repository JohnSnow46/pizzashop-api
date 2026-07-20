using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Api.Auth;

/// <summary>Response shape for <c>GET /api/auth/me</c> (api-layer.md 6.1) — read directly off <see cref="ICurrentUser"/>, no CQRS.</summary>
public sealed record CurrentUserDto(Guid? UserAccountId, UserRole? Role, Guid? CustomerId);
