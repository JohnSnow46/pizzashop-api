using PizzaShop.Application.Common.Exceptions;

namespace PizzaShop.Application.Identity;

/// <summary>
/// Single-use, time-limited token that authorizes resetting a <see cref="UserAccount"/>'s
/// password — application-level identity model living alongside <see cref="UserAccount"/> for
/// the same reason (ADR-0005: identity lives outside Domain). The redemption guard clause
/// (<see cref="Consume"/>) is the kind of state-dependent rule the project normally keeps in
/// Domain, but identity is a deliberate exception to that split.
/// </summary>
public class PasswordResetToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public Guid Id { get; }
    public Guid UserAccountId { get; }
    public string Token { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? UsedAt { get; private set; }

    // EF Core materialization only (ADR-0020) — not used by Application logic.
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid id, Guid userAccountId, string token, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserAccountId = userAccountId;
        Token = token;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public static PasswordResetToken Create(Guid userAccountId, DateTimeOffset now) =>
        new(Guid.NewGuid(), userAccountId, GenerateToken(), now, now + Lifetime);

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>Guard clause: a used or expired token can never be redeemed. Both cases report the
    /// same message deliberately — the caller must not be able to distinguish "already used" from
    /// "expired" from a token they already possess.</summary>
    public void Consume(DateTimeOffset now)
    {
        if (UsedAt is not null || now > ExpiresAt)
            throw new ConflictException("This password reset link is invalid or has expired.");
        UsedAt = now;
    }

    /// <summary>Superseded by a newer token for the same account — idempotent, never throws.</summary>
    public void Invalidate(DateTimeOffset now) => UsedAt ??= now;
}
