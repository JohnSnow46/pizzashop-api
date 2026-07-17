using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Orders.Queries;

public sealed class CheckDeliveryAvailabilityQueryHandler
    : IQueryHandler<CheckDeliveryAvailabilityQuery, DeliveryAvailabilityDto>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IGeocodingService _geocodingService;

    public CheckDeliveryAvailabilityQueryHandler(IRestaurantRepository restaurantRepository, IGeocodingService geocodingService)
    {
        _restaurantRepository = restaurantRepository;
        _geocodingService = geocodingService;
    }

    public async Task<DeliveryAvailabilityDto> Handle(CheckDeliveryAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        var address = new Address(
            query.Address.Street,
            query.Address.BuildingNumber,
            query.Address.City,
            query.Address.PostalCode,
            query.Address.ApartmentNumber,
            query.Address.Notes);

        var coordinate = await _geocodingService.GeocodeAsync(address, cancellationToken);
        if (coordinate is null)
            return new DeliveryAvailabilityDto(false, null, null);

        var isAvailable = restaurant.IsWithinDeliveryArea(coordinate);
        var distanceKm = restaurant.Location.DistanceKmTo(coordinate);

        return new DeliveryAvailabilityDto(
            isAvailable,
            distanceKm,
            isAvailable ? new MoneyDto(restaurant.DeliveryFee.Amount, restaurant.DeliveryFee.Currency) : null);
    }
}
