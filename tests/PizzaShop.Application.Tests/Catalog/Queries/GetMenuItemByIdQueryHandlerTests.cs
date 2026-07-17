using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Queries;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Queries;

public class GetMenuItemByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingItem_ReturnsDto()
    {
        var item = MenuItem.Create("Cola", MenuCategory.Drink, new Money(6));
        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var handler = new GetMenuItemByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMenuItemByIdQuery(item.Id), CancellationToken.None);

        result.Id.Should().Be(item.Id);
        result.Name.Should().Be("Cola");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);

        var handler = new GetMenuItemByIdQueryHandler(repository.Object);

        var act = () => handler.Handle(new GetMenuItemByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
