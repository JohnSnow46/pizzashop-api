using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Orders.Dtos;

public sealed record OrderItemDto(
    Guid Id,
    Guid MenuItemId,
    string MenuItemName,
    Guid? VariantId,
    string? VariantName,
    MoneyDto UnitPrice,
    int Quantity,
    string? Notes,
    IReadOnlyList<OrderItemExtraDto> Extras,
    MoneyDto LineTotal);
