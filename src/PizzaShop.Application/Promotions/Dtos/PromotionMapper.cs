using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

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
            promotion.UsageCount);

    private static MoneyDto? ToDto(Money? money) =>
        money is null ? null : new MoneyDto(money.Amount, money.Currency);
}
