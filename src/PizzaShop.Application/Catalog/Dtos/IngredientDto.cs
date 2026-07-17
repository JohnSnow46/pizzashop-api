using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Catalog.Dtos;

public sealed record IngredientDto(Guid Id, string Name, MoneyDto ExtraPrice, bool IsAvailable, string? Category);
