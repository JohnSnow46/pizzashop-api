using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Promotions.Dtos;

/// <summary>
/// DTO mirror of the <see cref="PizzaShop.Domain.Promotions.Promotion"/> aggregate, for the
/// management list (<c>GetPromotionsQuery</c>) — Queries never return Domain entities/VOs
/// directly (application-layer.md 4).
/// </summary>
public sealed record PromotionDto(
    Guid Id,
    string Name,
    string? Code,
    PromotionType Type,
    decimal? Value,
    MoneyDto? MinOrderValue,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    bool IsActive,
    int? UsageLimit,
    int UsageCount);
