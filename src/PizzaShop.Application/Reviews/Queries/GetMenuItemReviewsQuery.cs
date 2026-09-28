using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reviews.Dtos;

namespace PizzaShop.Application.Reviews.Queries;

/// <summary>Public — no auth (ADR-0042 §E). Catalog display for one menu item.</summary>
public sealed record GetMenuItemReviewsQuery(Guid MenuItemId) : IQuery<MenuItemReviewsDto>;
