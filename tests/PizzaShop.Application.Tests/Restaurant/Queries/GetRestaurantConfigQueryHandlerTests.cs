using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Restaurant.Queries;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Queries;

public class GetRestaurantConfigQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsConfigDtoMirroringRestaurant()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);

        var handler = new GetRestaurantConfigQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRestaurantConfigQuery(), CancellationToken.None);

        result.Id.Should().Be(restaurant.Id);
        result.Name.Should().Be(restaurant.Name);
        result.DeliveryRadiusKm.Should().Be(restaurant.DeliveryRadiusKm);
        result.TimeZoneId.Should().Be(restaurant.TimeZoneId);
        result.IsAcceptingOrders.Should().Be(restaurant.IsAcceptingOrders);
        result.DeliveryFee.Amount.Should().Be(restaurant.DeliveryFee.Amount);
        result.OpeningHours.Schedule[DayOfWeek.Monday].Should().ContainSingle();
    }
}
