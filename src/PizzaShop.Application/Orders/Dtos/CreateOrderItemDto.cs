namespace PizzaShop.Application.Orders.Dtos;

/// <summary>
/// A single cart line submitted with <c>CreateOrderCommand</c> (application-layer.md 4.3.1).
/// </summary>
public sealed record CreateOrderItemDto(
    Guid MenuItemId,
    Guid? VariantId,
    int Quantity,
    IReadOnlyList<Guid> ExtraIngredientIds,
    string? Notes = null);
