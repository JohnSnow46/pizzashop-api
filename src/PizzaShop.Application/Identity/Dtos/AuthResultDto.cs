using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Application.Identity.Dtos;

/// <summary>
/// Result of registration/login (api-layer.md 2.4) — the signed JWT plus enough claims data
/// for the client to render UI without decoding the token itself.
/// </summary>
public sealed record AuthResultDto(string Token, Guid UserAccountId, UserRole Role, Guid? CustomerId);
