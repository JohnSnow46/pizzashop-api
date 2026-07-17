using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Promotions.Commands;

/// <summary>
/// Creates a promotion (RestaurantAdmin, application-layer.md 4.5). <see cref="Type"/> ==
/// <c>PromotionType.BuyXGetY</c> is rejected by <c>CreatePromotionCommandValidator</c> — its
/// discount calculation is not implemented yet (ADR-0011), so we refuse to create such a
/// promotion in the first place rather than let it fail later at <c>CalculateDiscount</c>.
/// </summary>
public sealed record CreatePromotionCommand(
    string Name,
    PromotionType Type,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    decimal? Value,
    string? Code,
    MoneyDto? MinOrderValue,
    int? UsageLimit) : ICommand<Guid>;
