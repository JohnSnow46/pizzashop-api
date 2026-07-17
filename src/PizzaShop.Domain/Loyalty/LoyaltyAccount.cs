using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;

namespace PizzaShop.Domain.Loyalty;

/// <summary>
/// Loyalty points aggregate: current balance plus its append-only transaction history
/// (domain-model.md 7.1, ADR-0009). The conversion rate (how many points per order, per
/// zloty, etc.) is deliberately NOT modeled here — it lives behind <c>ILoyaltyPolicy</c>
/// in Application. This aggregate only records the resulting point movements and keeps
/// the balance consistent with its history.
/// </summary>
public class LoyaltyAccount
{
    private readonly List<LoyaltyTransaction> _transactions = new();

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public int PointsBalance { get; private set; }

    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();

    private LoyaltyAccount(Guid id, Guid customerId)
    {
        Id = id;
        CustomerId = customerId;
        PointsBalance = 0;
    }

    public static LoyaltyAccount Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        return new LoyaltyAccount(Guid.NewGuid(), customerId);
    }

    /// <summary>Records points earned, typically after an order reaches <c>Completed</c>.</summary>
    public void Earn(int points, string reason, DateTimeOffset occurredAt, Guid? orderId = null)
    {
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Earned points must be greater than zero.");

        AddTransaction(LoyaltyTransactionType.Earned, points, reason, orderId, occurredAt);
    }

    /// <summary>Spends points on an order. Balance can never go below zero (domain-model.md 7).</summary>
    public void Redeem(int points, string reason, DateTimeOffset occurredAt, Guid? orderId = null)
    {
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Redeemed points must be greater than zero.");
        if (points > PointsBalance)
            throw new InsufficientLoyaltyPointsException(points, PointsBalance);

        AddTransaction(LoyaltyTransactionType.Redeemed, -points, reason, orderId, occurredAt);
    }

    /// <summary>Manual correction (positive or negative), e.g. customer service goodwill or fix.</summary>
    public void Adjust(int points, string reason, DateTimeOffset occurredAt)
    {
        if (points == 0)
            throw new ArgumentException("Adjustment points must not be zero.", nameof(points));
        if (points < 0 && -points > PointsBalance)
            throw new InsufficientLoyaltyPointsException(-points, PointsBalance);

        AddTransaction(LoyaltyTransactionType.Adjusted, points, reason, null, occurredAt);
    }

    /// <summary>Removes points that have expired (mechanism/schedule decided outside Domain, ADR-0009).</summary>
    public void Expire(int points, string reason, DateTimeOffset occurredAt)
    {
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Expired points must be greater than zero.");
        if (points > PointsBalance)
            throw new InsufficientLoyaltyPointsException(points, PointsBalance);

        AddTransaction(LoyaltyTransactionType.Expired, -points, reason, null, occurredAt);
    }

    private void AddTransaction(LoyaltyTransactionType type, int signedPoints, string reason, Guid? orderId, DateTimeOffset occurredAt)
    {
        var transaction = LoyaltyTransaction.Create(type, signedPoints, reason, orderId, occurredAt);
        _transactions.Add(transaction);
        PointsBalance += signedPoints;
    }
}
