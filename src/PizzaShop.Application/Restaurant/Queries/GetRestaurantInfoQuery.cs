using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Application.Restaurant.Queries;

public sealed record GetRestaurantInfoQuery : IQuery<RestaurantInfoDto>;
