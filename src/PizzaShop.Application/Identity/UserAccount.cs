using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Application.Identity;

/// <summary>
/// Application-level identity model (ADR-0026, api-layer.md 2.2) — deliberately NOT a Domain
/// aggregate (ADR-0005: identity lives outside Domain). Persisted by Infrastructure via EF
/// Core (<see cref="Abstractions.IUserAccountRepository"/>); a JWT is minted from it by
/// <see cref="Abstractions.IJwtTokenGenerator"/> (implemented in Api, ADR-0024).
/// </summary>
public class UserAccount
{
    public Guid Id { get; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    // EF Core materialization only (ADR-0020) — not used by Application logic.
    private UserAccount()
    {
    }

    private UserAccount(Guid id, string email, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static UserAccount Create(string email, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new UserAccount(Guid.NewGuid(), NormalizeEmail(email), passwordHash, role, createdAt);
    }

    /// <summary>
    /// Canonical form used for both storage and lookup, so the same address always resolves
    /// to the same account regardless of casing/surrounding whitespace.
    /// </summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
