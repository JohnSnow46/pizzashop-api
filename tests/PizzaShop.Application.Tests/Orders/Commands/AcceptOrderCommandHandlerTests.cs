using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Orders.Commands;

public class AcceptOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderNotifier> _orderNotifier = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptOrderCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _orderNotifier.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_PendingOrder_AcceptsAndNotifies()
    {
        var order = OrderTestFactory.CreateOrder();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(new AcceptOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Accepted);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Accepted, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEstimatedReadyAt_SetsEstimatedReadyAt()
    {
        var order = OrderTestFactory.CreateOrder();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var estimatedReadyAt = order.PlacedAt.AddMinutes(30);

        var handler = CreateHandler();

        await handler.Handle(new AcceptOrderCommand(order.Id, estimatedReadyAt), CancellationToken.None);

        order.EstimatedReadyAt.Should().Be(estimatedReadyAt);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new AcceptOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
