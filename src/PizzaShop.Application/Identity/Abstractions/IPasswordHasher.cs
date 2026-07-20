namespace PizzaShop.Application.Identity.Abstractions;

/// <summary>
/// Password hashing port (api-layer.md 2.3, ADR-0026). Implemented by
/// <c>BcryptPasswordHasher</c> in Infrastructure (BCrypt.Net-Next) — a plain util, like
/// <c>IClock</c>, not tied to persistence.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
