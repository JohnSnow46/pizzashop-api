using PizzaShop.Domain.Enums;

namespace PizzaShop.Domain.Loyalty;

/// <summary>
/// Immutable, append-only record of a loyalty points movement (domain-model.md 7.2,
/// ADR-0009). <see cref="Points"/> is positive for <c>Earned</c>/positive <c>Adjusted</c>
/// entries and negative for <c>Redeemed</c>/<c>Expired</c>/negative <c>Adjusted</c> entries.
/// </summary>
public class LoyaltyTransaction
{
    public Guid Id { get; }
    public LoyaltyTransactionType Type { get; }
    public int Points { get; }
    public string Reason { get; }
    public Guid? OrderId { get; }
    public DateTimeOffset OccurredAt { get; }

    private LoyaltyTransaction(
        Guid id,
        LoyaltyTransactionType type,
        int points,
        string reason,
        Guid? orderId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Points = points;
        Reason = reason;
        OrderId = orderId;
        OccurredAt = occurredAt;
    }

    internal static LoyaltyTransaction Create(
        LoyaltyTransactionType type,
        int points,
        string reason,
        Guid? orderId,
        DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));
        if (points == 0)
            throw new ArgumentException("Points must not be zero.", nameof(points));

        return new LoyaltyTransaction(Guid.NewGuid(), type, points, reason, orderId, occurredAt);
    }
}
