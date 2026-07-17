using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Commands;

public sealed record CreateIngredientCommand(string Name, MoneyDto ExtraPrice, string? Category) : ICommand<Guid>;
