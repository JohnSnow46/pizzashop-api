namespace PizzaShop.Application.Common.Abstractions;

/// <summary>
/// Already-authenticated request context, supplied by Api (ADR-0004/ADR-0005). Null
/// members mean an anonymous/guest request. Role-based authorization stays in Api
/// (CLAUDE.md, ADR-0004) — handlers only use these values to scope data (e.g. "own order").
/// </summary>
public interface ICurrentUser
{
    Guid? UserAccountId { get; }

    Guid? CustomerId { get; }

    UserRole? Role { get; }
}
