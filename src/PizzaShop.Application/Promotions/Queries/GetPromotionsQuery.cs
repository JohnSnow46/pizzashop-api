using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;

namespace PizzaShop.Application.Promotions.Queries;

/// <summary>List of all promotions for the management screen (RestaurantAdmin, application-layer.md 4.5).</summary>
public sealed record GetPromotionsQuery : IQuery<IReadOnlyList<PromotionDto>>;
