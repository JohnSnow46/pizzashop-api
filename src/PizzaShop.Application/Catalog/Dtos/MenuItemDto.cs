using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Catalog.Dtos;

public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string? Description,
    MenuCategory Category,
    MoneyDto BasePrice,
    bool IsAvailable,
    string? ImageUrl,
    IReadOnlyList<MenuItemVariantDto> Variants,
    IReadOnlyList<IngredientDto> BaseIngredients,
    IReadOnlyList<IngredientDto> AllowedExtras);
