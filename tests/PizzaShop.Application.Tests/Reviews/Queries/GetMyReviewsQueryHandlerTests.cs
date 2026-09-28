using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Reviews.Queries;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Tests.Reviews.Queries;

public class GetMyReviewsQueryHandlerTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private GetMyReviewsQueryHandler CreateHandler() => new(_reviewRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NoCurrentCustomer_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new GetMyReviewsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_CurrentCustomer_ReturnsOnlyTheirReviews()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        var reviews = new[] { Review.Create(Guid.NewGuid(), customerId, Guid.NewGuid(), 5, "Great", Now) };
        _reviewRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyReviewsQuery(), CancellationToken.None);

        result.Should().ContainSingle(r => r.CustomerId == customerId);
    }
}
