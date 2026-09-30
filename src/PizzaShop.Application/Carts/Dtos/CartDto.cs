namespace PizzaShop.Application.Carts.Dtos;

public sealed record CartDto(IReadOnlyList<CartItemDto> Items, DateTimeOffset UpdatedAt);

public sealed record CartItemDto(
    Guid Id, Guid MenuItemId, Guid? VariantId, IReadOnlyList<Guid> ExtraIds, int Quantity, string? Notes);
