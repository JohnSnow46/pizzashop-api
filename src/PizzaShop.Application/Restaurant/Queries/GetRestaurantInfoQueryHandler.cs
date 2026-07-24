using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Application.Restaurant.Queries;

public sealed class GetRestaurantInfoQueryHandler : IQueryHandler<GetRestaurantInfoQuery, RestaurantInfoDto>
{
    private readonly IRestaurantRepository _restaurantRepository;

    public GetRestaurantInfoQueryHandler(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<RestaurantInfoDto> Handle(GetRestaurantInfoQuery query, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);
        return RestaurantMapper.ToInfoDto(restaurant);
    }
}
