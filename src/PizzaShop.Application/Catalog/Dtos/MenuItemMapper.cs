using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Catalog.Dtos;

internal static class MenuItemMapper
{
    public static MenuItemDto ToDto(MenuItem item) =>
        new(
            item.Id,
            item.Name,
            item.Description,
            item.Category,
            new MoneyDto(item.BasePrice.Amount, item.BasePrice.Currency),
            item.IsAvailable,
            item.ImageUrl,
            item.Variants.Select(ToDto).ToList(),
            item.BaseIngredients.Select(ToDto).ToList(),
            item.AllowedExtras.Select(ToDto).ToList());

    public static MenuItemVariantDto ToDto(MenuItemVariant variant) =>
        new(variant.Id, variant.Name, new MoneyDto(variant.Price.Amount, variant.Price.Currency), variant.IsDefault);

    public static IngredientDto ToDto(Ingredient ingredient) =>
        new(
            ingredient.Id,
            ingredient.Name,
            new MoneyDto(ingredient.ExtraPrice.Amount, ingredient.ExtraPrice.Currency),
            ingredient.IsAvailable,
            ingredient.Category);
}
