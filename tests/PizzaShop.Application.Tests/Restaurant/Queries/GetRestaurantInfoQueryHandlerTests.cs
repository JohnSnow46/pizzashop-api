using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Restaurant.Queries;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Queries;

public class GetRestaurantInfoQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsInfoDtoMirroringRestaurant()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);

        var handler = new GetRestaurantInfoQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRestaurantInfoQuery(), CancellationToken.None);

        result.Name.Should().Be(restaurant.Name);
        result.Address.Street.Should().Be(restaurant.Address.Street);
        result.Address.City.Should().Be(restaurant.Address.City);
        result.OpeningHours.Schedule[DayOfWeek.Monday].Should().ContainSingle();
    }
}
