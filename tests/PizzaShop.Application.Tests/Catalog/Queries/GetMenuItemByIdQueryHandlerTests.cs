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

        var reviewRepository = new Mock<IReviewRepository>();
        reviewRepository
            .Setup(r => r.GetRatingSummariesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (double Average, int Count)>());

        var handler = new GetMenuItemByIdQueryHandler(repository.Object, reviewRepository.Object);

        var result = await handler.Handle(new GetMenuItemByIdQuery(item.Id), CancellationToken.None);

        result.Id.Should().Be(item.Id);
        result.Name.Should().Be("Cola");
        result.AverageRating.Should().BeNull();
        result.ReviewCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ItemWithReviews_PopulatesAverageRatingAndCount()
    {
        var item = MenuItem.Create("Cola", MenuCategory.Drink, new Money(6));
        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var reviewRepository = new Mock<IReviewRepository>();
        reviewRepository
            .Setup(r => r.GetRatingSummariesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (double Average, int Count)> { [item.Id] = (3.0, 1) });

        var handler = new GetMenuItemByIdQueryHandler(repository.Object, reviewRepository.Object);

        var result = await handler.Handle(new GetMenuItemByIdQuery(item.Id), CancellationToken.None);

        result.AverageRating.Should().Be(3.0);
        result.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);

        var reviewRepository = new Mock<IReviewRepository>();

        var handler = new GetMenuItemByIdQueryHandler(repository.Object, reviewRepository.Object);

        var act = () => handler.Handle(new GetMenuItemByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
