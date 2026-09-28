using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Reviews.Queries;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Tests.Reviews.Queries;

public class GetMenuItemReviewsQueryHandlerTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private GetMenuItemReviewsQueryHandler CreateHandler() => new(_reviewRepository.Object);

    [Fact]
    public async Task Handle_NoReviews_ReturnsEmptyListAndNullAverage()
    {
        var menuItemId = Guid.NewGuid();
        _reviewRepository
            .Setup(r => r.GetByMenuItemIdAsync(menuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Review>());

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMenuItemReviewsQuery(menuItemId), CancellationToken.None);

        result.MenuItemId.Should().Be(menuItemId);
        result.AverageRating.Should().BeNull();
        result.Reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithReviews_ReturnsThemAndCorrectAverage()
    {
        var menuItemId = Guid.NewGuid();
        var reviews = new[]
        {
            Review.Create(Guid.NewGuid(), Guid.NewGuid(), menuItemId, 4, "Good", Now),
            Review.Create(Guid.NewGuid(), Guid.NewGuid(), menuItemId, 2, null, Now),
        };
        _reviewRepository
            .Setup(r => r.GetByMenuItemIdAsync(menuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMenuItemReviewsQuery(menuItemId), CancellationToken.None);

        result.AverageRating.Should().Be(3.0);
        result.Reviews.Should().HaveCount(2);
    }
}
