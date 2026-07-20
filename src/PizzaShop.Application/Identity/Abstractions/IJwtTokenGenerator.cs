namespace PizzaShop.Application.Identity.Abstractions;

/// <summary>
/// Issues a signed JWT for an authenticated <see cref="UserAccount"/> (api-layer.md 2.3/2.7,
/// ADR-0026). Implemented in Api (<c>JwtTokenGenerator</c>), not Infrastructure — it needs the
/// signing configuration, symmetrically to <c>ICurrentUser</c> reading the resulting claims
/// back (ADR-0024). <paramref name="customerId"/> is only non-null for a
/// <see cref="Common.Abstractions.UserRole.Customer"/> account (api-layer.md 2.5) and ends up
/// as the <c>customerId</c> claim, so <c>ICurrentUser.CustomerId</c> never needs a database
/// round-trip per request.
/// </summary>
public interface IJwtTokenGenerator
{
    string Generate(UserAccount account, Guid? customerId);
}
