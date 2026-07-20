using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Loyalty;

/// <summary>
/// Placeholder <see cref="ILoyaltyPolicy"/> implementation (ADR-0014): 1 point earned per
/// 1 PLN of subtotal, 1 point worth 0.05 PLN when redeemed. The business has not finalized
/// the real conversion rate yet — swapping this out for the eventual rule requires no changes
/// to Domain or the handlers that call this port.
/// </summary>
public sealed class LinearLoyaltyPolicy : ILoyaltyPolicy
{
    private const decimal PointsEarnedPerCurrencyUnit = 1m;
    private const decimal RedemptionValuePerPoint = 0.05m;

    public int CalculatePointsToEarn(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return (int)Math.Floor(order.Subtotal.Amount * PointsEarnedPerCurrencyUnit);
    }

    public Money CalculateRedemptionValue(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Points cannot be negative.");

        return new Money(points * RedemptionValuePerPoint);
    }

    public int MaxRedeemablePoints(Order order, int balance)
    {
        ArgumentNullException.ThrowIfNull(order);

        return Math.Max(0, balance);
    }
}
