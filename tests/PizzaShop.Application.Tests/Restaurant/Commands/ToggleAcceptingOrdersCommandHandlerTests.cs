using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Commands;

public class ToggleAcceptingOrdersCommandHandlerTests
{
    [Fact]
    public async Task Handle_StopAcceptingOrders_UpdatesFlagAndPersists()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ToggleAcceptingOrdersCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(new ToggleAcceptingOrdersCommand(false), CancellationToken.None);

        restaurant.IsAcceptingOrders.Should().BeFalse();
        repository.Verify(r => r.UpdateAsync(restaurant, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StartAcceptingOrders_UpdatesFlagAndPersists()
    {
        var restaurant = RestaurantTestFactory.Create();
        restaurant.StopAcceptingOrders();

        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ToggleAcceptingOrdersCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(new ToggleAcceptingOrdersCommand(true), CancellationToken.None);

        restaurant.IsAcceptingOrders.Should().BeTrue();
    }
}
