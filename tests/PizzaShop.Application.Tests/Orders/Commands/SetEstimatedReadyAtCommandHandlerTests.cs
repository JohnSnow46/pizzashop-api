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

public class SetEstimatedReadyAtCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderNotifier> _orderNotifier = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SetEstimatedReadyAtCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _orderNotifier.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_AcceptedOrder_SetsEstimatedReadyAtAndNotifies()
    {
        var order = OrderTestFactory.CreateOrder();
        order.Accept();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var estimatedReadyAt = order.PlacedAt.AddMinutes(45);

        var handler = CreateHandler();

        await handler.Handle(new SetEstimatedReadyAtCommand(order.Id, estimatedReadyAt), CancellationToken.None);

        order.EstimatedReadyAt.Should().Be(estimatedReadyAt);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Accepted, estimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new SetEstimatedReadyAtCommand(Guid.NewGuid(), DateTimeOffset.UtcNow), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
