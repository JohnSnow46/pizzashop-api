namespace PizzaShop.Application.Promotions.Dtos;

/// <summary>
/// DTO mirror of the <see cref="PizzaShop.Domain.Promotions.BuyXGetYRule"/> Value Object, used
/// by <c>CreatePromotionCommand</c> to configure a <c>PromotionType.BuyXGetY</c> promotion
/// (domain-model.md 8.2, ADR-0034).
/// </summary>
public sealed record BuyXGetYRuleDto(
    Guid TriggerMenuItemId,
    int BuyQuantity,
    Guid RewardMenuItemId,
    int GetQuantity,
    decimal RewardDiscountPercentage);
