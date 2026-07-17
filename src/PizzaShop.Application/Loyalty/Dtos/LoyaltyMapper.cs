using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Loyalty.Dtos;

internal static class LoyaltyMapper
{
    public static LoyaltyBalanceDto ToDto(LoyaltyAccount account) =>
        new(
            account.PointsBalance,
            account.Transactions
                .Select(t => new LoyaltyTransactionDto(t.Type, t.Points, t.Reason, t.OrderId, t.OccurredAt))
                .ToList());
}
