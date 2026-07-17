using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Commands;

public class SetMenuItemAvailabilityCommandHandlerTests
{
    [Fact]
    public async Task Handle_MarkAvailableTrue_MarksMenuItemAvailable()
    {
        var menuItem = MenuItem.Create("Cola", MenuCategory.Drink, new Money(5));
        menuItem.MarkUnavailable();

        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new SetMenuItemAvailabilityCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(new SetMenuItemAvailabilityCommand(menuItem.Id, true), CancellationToken.None);

        menuItem.IsAvailable.Should().BeTrue();
        repository.Verify(r => r.UpdateAsync(menuItem, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MenuItemNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new SetMenuItemAvailabilityCommandHandler(repository.Object, unitOfWork.Object);

        var act = () => handler.Handle(new SetMenuItemAvailabilityCommand(Guid.NewGuid(), false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
