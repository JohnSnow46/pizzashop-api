using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Promotions.Dtos;

/// <summary>
/// Result of <c>ValidatePromotionCodeQuery</c> (application-layer.md 4.5) — a preview of the
/// discount a coupon code would apply to the given cart, without applying it.
/// <see cref="DiscountAmount"/> is <c>null</c> when <see cref="IsQualified"/> is <c>false</c>.
/// </summary>
public sealed record PromotionDiscountPreviewDto(bool IsQualified, MoneyDto? DiscountAmount);
