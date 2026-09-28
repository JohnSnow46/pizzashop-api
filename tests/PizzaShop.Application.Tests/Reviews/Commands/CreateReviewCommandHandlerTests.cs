using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Reviews.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Tests.Reviews.Commands;

public class CreateReviewCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public CreateReviewCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private CreateReviewCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _reviewRepository.Object, _unitOfWork.Object, _currentUser.Object, _clock.Object);

    /// <summary>A Completed, OnPickup order for <paramref name="customerId"/> with one item.</summary>
    private static (Domain.Orders.Order Order, Guid MenuItemId) CompletedOrderFor(Guid customerId)
    {
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        var menuItemId = order.Items.Single().MenuItemId;

        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        order.Complete();

        return (order, menuItemId);
    }

    [Fact]
    public async Task Handle_NoCurrentCustomer_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_OrderDoesNotExist_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrderBelongsToDifferentCustomer_ThrowsNotFoundException()
    {
        var (order, menuItemId) = CompletedOrderFor(Guid.NewGuid());
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(order.Id, menuItemId, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrderNotCompleted_ThrowsConflictException()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId); // PendingAcceptance
        var menuItemId = order.Items.Single().MenuItemId;
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(order.Id, menuItemId, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_MenuItemNotInOrder_ThrowsConflictException()
    {
        var customerId = Guid.NewGuid();
        var (order, _) = CompletedOrderFor(customerId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(order.Id, Guid.NewGuid(), 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflictException()
    {
        var customerId = Guid.NewGuid();
        var (order, menuItemId) = CompletedOrderFor(customerId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reviewRepository
            .Setup(r => r.GetByOrderAndMenuItemAsync(order.Id, menuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Review.Create(order.Id, customerId, menuItemId, 3, null, Now));

        var handler = CreateHandler();

        var act = () => handler.Handle(new CreateReviewCommand(order.Id, menuItemId, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsReviewAndReturnsId()
    {
        var customerId = Guid.NewGuid();
        var (order, menuItemId) = CompletedOrderFor(customerId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reviewRepository
            .Setup(r => r.GetByOrderAndMenuItemAsync(order.Id, menuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        Review? added = null;
        _reviewRepository
            .Setup(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Callback<Review, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var result = await handler.Handle(new CreateReviewCommand(order.Id, menuItemId, 4, "Very good"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.OrderId.Should().Be(order.Id);
        added.CustomerId.Should().Be(customerId);
        added.MenuItemId.Should().Be(menuItemId);
        added.Rating.Should().Be(4);
        added.Comment.Should().Be("Very good");
        result.Should().Be(added.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
