using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Orders.Commands;

public class RejectOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderNotifier> _orderNotifier = new();
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public RejectOrderCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    private RejectOrderCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _orderNotifier.Object, _loyaltyAccountRepository.Object, _unitOfWork.Object, _clock.Object);

    [Fact]
    public async Task Handle_PendingOrder_RejectsAndNotifies()
    {
        var order = OrderTestFactory.CreateOrder();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(new RejectOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Rejected);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Rejected, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RejectOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrderWithRedeemedPoints_ReversesLoyaltyPoints()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        order.RedeemLoyaltyPoints(50, new Money(2m));
        var loyaltyAccount = LoyaltyAccount.Create(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loyaltyAccount);

        var handler = CreateHandler();

        await handler.Handle(new RejectOrderCommand(order.Id), CancellationToken.None);

        loyaltyAccount.PointsBalance.Should().Be(50);
        loyaltyAccount.Transactions.Should().ContainSingle(t => t.Type == Domain.Enums.LoyaltyTransactionType.Reversed && t.Points == 50);
        _loyaltyAccountRepository.Verify(r => r.UpdateAsync(loyaltyAccount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderWithoutRedeemedPoints_NeverTouchesLoyaltyAccount()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(new RejectOrderCommand(order.Id), CancellationToken.None);

        _loyaltyAccountRepository.Verify(r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _loyaltyAccountRepository.Verify(r => r.UpdateAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
