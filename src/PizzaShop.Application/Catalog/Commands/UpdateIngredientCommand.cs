using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Commands;

public sealed record UpdateIngredientCommand(Guid Id, string Name, MoneyDto ExtraPrice, bool IsAvailable) : ICommand;
