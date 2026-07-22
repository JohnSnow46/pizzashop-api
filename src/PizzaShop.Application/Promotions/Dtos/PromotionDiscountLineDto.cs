using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Promotions.Dtos;

/// <summary>
/// One cart line submitted with <c>ValidatePromotionCodeQuery</c> — mirrors
/// <c>PizzaShop.Domain.Promotions.OrderDiscountLine</c> so the preview can build the same
/// <c>OrderDiscountContext</c> the order-creation handler uses (domain-model.md 8.2, ADR-0034).
/// Unused by discount types that don't depend on line items (Percentage/FixedAmount/FreeDelivery).
/// </summary>
public sealed record PromotionDiscountLineDto(Guid MenuItemId, MoneyDto UnitPrice, int Quantity);
