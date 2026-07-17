namespace PizzaShop.Application.Loyalty.Dtos;

/// <summary>
/// DTO mirror of a customer's <see cref="PizzaShop.Domain.Loyalty.LoyaltyAccount"/> — balance
/// plus its append-only transaction history (application-layer.md 4.6, ADR-0009).
/// </summary>
public sealed record LoyaltyBalanceDto(int PointsBalance, IReadOnlyList<LoyaltyTransactionDto> Transactions);
