using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;

namespace PizzaShop.Application.Promotions.Queries;

/// <summary>
/// Checks whether a coupon code qualifies for a cart (application-layer.md 4.5) and returns a
/// preview of the discount, without applying it — actual application happens inline in
/// <c>CreateOrderCommand</c> (step 6).
/// </summary>
public sealed record ValidatePromotionCodeQuery(string Code, MoneyDto Subtotal, MoneyDto DeliveryFee)
    : IQuery<PromotionDiscountPreviewDto>;
