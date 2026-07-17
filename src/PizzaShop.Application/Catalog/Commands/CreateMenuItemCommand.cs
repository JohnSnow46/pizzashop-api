using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Catalog.Commands;

public sealed record CreateMenuItemCommand(
    string Name,
    MenuCategory Category,
    MoneyDto BasePrice,
    string? Description,
    string? ImageUrl,
    IReadOnlyList<Guid> BaseIngredientIds,
    IReadOnlyList<Guid> AllowedExtraIds,
    IReadOnlyList<MenuItemVariantInputDto> Variants) : ICommand<Guid>;
