using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Abstractions.Loyalty;

/// <summary>
/// Policy port for the loyalty points conversion rate (ADR-0009/ADR-0014). Deliberately
/// holds no concrete rule — <c>LoyaltyAccount</c> and <c>Order</c> (Domain) only record the
/// resulting point movements; the actual "how many points per zloty" rule is implemented in
/// Infrastructure and can change without touching Domain or the handlers that call this port.
/// </summary>
public interface ILoyaltyPolicy
{
    /// <summary>
    /// Points to award once <paramref name="order"/> reaches <c>OrderStatus.Completed</c>
    /// (wired into <c>CompleteOrderCommand</c>). Result is recorded via <c>Order.SetPointsToEarn</c>.
    /// </summary>
    int CalculatePointsToEarn(Order order);

    /// <summary>
    /// Monetary value of redeeming <paramref name="points"/> (wired into <c>CreateOrderCommand</c>
    /// step 7). Result feeds <c>Order.RedeemLoyaltyPoints</c>/<c>LoyaltyAccount.Redeem</c>.
    /// </summary>
    Money CalculateRedemptionValue(int points);

    /// <summary>
    /// Upper bound on how many points may be redeemed against <paramref name="order"/>, given
    /// the customer's current <paramref name="balance"/> (e.g. "points may cover at most 50% of
    /// the order value"). Optional guard rail — not every implementation needs to cap this
    /// beyond the balance itself.
    /// </summary>
    int MaxRedeemablePoints(Order order, int balance);
}
