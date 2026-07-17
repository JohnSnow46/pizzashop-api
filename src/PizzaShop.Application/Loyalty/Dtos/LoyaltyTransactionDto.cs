using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Loyalty.Dtos;

/// <summary>DTO mirror of a single <see cref="PizzaShop.Domain.Loyalty.LoyaltyTransaction"/>.</summary>
public sealed record LoyaltyTransactionDto(
    LoyaltyTransactionType Type,
    int Points,
    string Reason,
    Guid? OrderId,
    DateTimeOffset OccurredAt);
