using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Commands;

public class UpdateOrderingThresholdsCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesThresholdsAndPersists()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateOrderingThresholdsCommandHandler(repository.Object, unitOfWork.Object);

        var command = new UpdateOrderingThresholdsCommand(
            new MoneyDto(30, "PLN"),
            new MoneyDto(80, "PLN"),
            new MoneyDto(5, "PLN"));

        await handler.Handle(command, CancellationToken.None);

        restaurant.MinimumOrderValue!.Amount.Should().Be(30);
        restaurant.FreeDeliveryThreshold!.Amount.Should().Be(80);
        restaurant.DeliveryFee.Amount.Should().Be(5);
        repository.Verify(r => r.UpdateAsync(restaurant, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullOptionalThresholds_ClearsThem()
    {
        var restaurant = RestaurantTestFactory.Create();
        restaurant.UpdateOrderingThresholds(new(30), new(80), new(5));

        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateOrderingThresholdsCommandHandler(repository.Object, unitOfWork.Object);

        var command = new UpdateOrderingThresholdsCommand(null, null, new MoneyDto(6, "PLN"));

        await handler.Handle(command, CancellationToken.None);

        restaurant.MinimumOrderValue.Should().BeNull();
        restaurant.FreeDeliveryThreshold.Should().BeNull();
        restaurant.DeliveryFee.Amount.Should().Be(6);
    }
}
