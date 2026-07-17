using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Application.Restaurant.Queries;

public sealed class GetRestaurantConfigQueryHandler : IQueryHandler<GetRestaurantConfigQuery, RestaurantConfigDto>
{
    private readonly IRestaurantRepository _restaurantRepository;

    public GetRestaurantConfigQueryHandler(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<RestaurantConfigDto> Handle(GetRestaurantConfigQuery query, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);
        return RestaurantMapper.ToDto(restaurant);
    }
}
