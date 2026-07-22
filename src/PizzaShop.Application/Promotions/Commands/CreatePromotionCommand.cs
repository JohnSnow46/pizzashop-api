using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Promotions.Commands;

/// <summary>
/// Creates a promotion (RestaurantAdmin, application-layer.md 4.5). <see cref="BuyXGetY"/> is
/// required (and <see cref="Value"/> must be <c>null</c>) when <see cref="Type"/> ==
/// <c>PromotionType.BuyXGetY</c>; for every other type <see cref="BuyXGetY"/> must be
/// <c>null</c> — enforced by <c>CreatePromotionCommandValidator</c> and, redundantly, by
/// <c>Promotion.Create</c> (domain-model.md 8.2, ADR-0034).
/// </summary>
public sealed record CreatePromotionCommand(
    string Name,
    PromotionType Type,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    decimal? Value,
    string? Code,
    MoneyDto? MinOrderValue,
    int? UsageLimit,
    BuyXGetYRuleDto? BuyXGetY = null) : ICommand<Guid>;
