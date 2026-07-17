using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Orders.Dtos;

public sealed record OrderItemExtraDto(Guid IngredientId, string Name, MoneyDto Price);
