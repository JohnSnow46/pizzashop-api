using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Dtos;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Restaurant.Commands;

public class UpdateOpeningHoursCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesOpeningHoursAndPersists()
    {
        var restaurant = RestaurantTestFactory.Create();
        var repository = new Mock<IRestaurantRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restaurant);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateOpeningHoursCommandHandler(repository.Object, unitOfWork.Object);

        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>
        {
            [DayOfWeek.Tuesday] = new List<TimeRangeDto> { new(new TimeOnly(9, 0), new TimeOnly(21, 0)) },
        };

        await handler.Handle(new UpdateOpeningHoursCommand(new OpeningHoursDto(schedule)), CancellationToken.None);

        restaurant.OpeningHours.RangesFor(DayOfWeek.Tuesday).Should().ContainSingle();
        restaurant.OpeningHours.RangesFor(DayOfWeek.Monday).Should().BeEmpty();
        repository.Verify(r => r.UpdateAsync(restaurant, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
