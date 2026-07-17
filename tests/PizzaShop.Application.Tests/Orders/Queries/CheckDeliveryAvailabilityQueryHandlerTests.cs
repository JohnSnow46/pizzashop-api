using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Orders.Queries;

public class CheckDeliveryAvailabilityQueryHandlerTests
{
    private readonly Mock<IRestaurantRepository> _restaurantRepository = new();
    private readonly Mock<IGeocodingService> _geocodingService = new();

    private static readonly AddressDto Address = new("Client St", "2", "Warsaw", "00-002");

    private CheckDeliveryAvailabilityQueryHandler CreateHandler() =>
        new(_restaurantRepository.Object, _geocodingService.Object);

    [Fact]
    public async Task Handle_AddressWithinRadius_ReturnsAvailableWithDeliveryFee()
    {
        var restaurant = OrderTestFactory.CreateOpenRestaurant();
        _restaurantRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderTestFactory.NearbyPoint);

        var handler = CreateHandler();

        var result = await handler.Handle(new CheckDeliveryAvailabilityQuery(Address), CancellationToken.None);

        result.IsAvailable.Should().BeTrue();
        result.DistanceKm.Should().NotBeNull();
        result.DeliveryFee.Should().Be(new MoneyDto(restaurant.DeliveryFee.Amount, restaurant.DeliveryFee.Currency));
    }

    [Fact]
    public async Task Handle_AddressOutsideRadius_ReturnsUnavailableWithoutFee()
    {
        var restaurant = OrderTestFactory.CreateOpenRestaurant();
        _restaurantRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(50.0647, 19.9450)); // Krakow, far outside the radius

        var handler = CreateHandler();

        var result = await handler.Handle(new CheckDeliveryAvailabilityQuery(Address), CancellationToken.None);

        result.IsAvailable.Should().BeFalse();
        result.DeliveryFee.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AddressCannotBeGeocoded_ReturnsUnavailable()
    {
        var restaurant = OrderTestFactory.CreateOpenRestaurant();
        _restaurantRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeoCoordinate?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new CheckDeliveryAvailabilityQuery(Address), CancellationToken.None);

        result.IsAvailable.Should().BeFalse();
        result.DistanceKm.Should().BeNull();
        result.DeliveryFee.Should().BeNull();
    }
}
