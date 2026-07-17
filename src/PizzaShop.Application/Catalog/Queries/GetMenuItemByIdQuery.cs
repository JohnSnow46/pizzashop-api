using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Queries;

public sealed record GetMenuItemByIdQuery(Guid Id) : IQuery<MenuItemDto>;
