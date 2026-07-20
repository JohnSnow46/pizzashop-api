using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Infrastructure.Identity;

/// <summary>
/// <see cref="IPasswordHasher"/> implementation using BCrypt.Net-Next (ADR-0026 — own
/// UserAccount + BCrypt instead of ASP.NET Core Identity). Stateless util, like
/// <c>SystemClock</c> — registered as a singleton.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
