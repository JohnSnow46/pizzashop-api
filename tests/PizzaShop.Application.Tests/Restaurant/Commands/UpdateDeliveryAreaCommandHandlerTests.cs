using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Commands;

public class UpdateDeliveryAreaCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesLocationAndRadiusAndPersists()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateDeliveryAreaCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(new UpdateDeliveryAreaCommand(50.0614, 19.9366, 8), CancellationToken.None);

        restaurant.Location.Latitude.Should().Be(50.0614);
        restaurant.Location.Longitude.Should().Be(19.9366);
        restaurant.DeliveryRadiusKm.Should().Be(8);
        repository.Verify(r => r.UpdateAsync(restaurant, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
