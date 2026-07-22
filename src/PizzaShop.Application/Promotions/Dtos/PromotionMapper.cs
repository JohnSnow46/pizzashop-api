using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;
using DomainBuyXGetYRule = PizzaShop.Domain.Promotions.BuyXGetYRule;

namespace PizzaShop.Application.Promotions.Dtos;

internal static class PromotionMapper
{
    public static PromotionDto ToDto(Promotion promotion) =>
        new(
            promotion.Id,
            promotion.Name,
            promotion.Code,
            promotion.Type,
            promotion.Value,
            ToDto(promotion.MinOrderValue),
            promotion.ValidFrom,
            promotion.ValidTo,
            promotion.IsActive,
            promotion.UsageLimit,
            promotion.UsageCount,
            ToDto(promotion.BuyXGetYRule));

    private static MoneyDto? ToDto(Money? money) =>
        money is null ? null : new MoneyDto(money.Amount, money.Currency);

    private static BuyXGetYRuleDto? ToDto(DomainBuyXGetYRule? rule) =>
        rule is null
            ? null
            : new BuyXGetYRuleDto(rule.TriggerMenuItemId, rule.BuyQuantity, rule.RewardMenuItemId, rule.GetQuantity, rule.RewardDiscountPercentage);
}
