using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Tests.Orders.Commands;

public class CompleteOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderNotifier> _orderNotifier = new();
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<ILoyaltyPolicy> _loyaltyPolicy = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public CompleteOrderCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    private CompleteOrderCommandHandler CreateHandler() =>
        new(
            _orderRepository.Object,
            _orderNotifier.Object,
            _loyaltyAccountRepository.Object,
            _loyaltyPolicy.Object,
            _unitOfWork.Object,
            _clock.Object);

    [Fact]
    public async Task Handle_ReadyPickupOrder_CompletesAndNotifies()
    {
        var order = OrderTestFactory.CreateOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Completed);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Completed, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CompleteOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CustomerOrder_AwardsLoyaltyPoints()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var loyaltyAccount = LoyaltyAccount.Create(customerId);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loyaltyAccount);
        _loyaltyPolicy.Setup(p => p.CalculatePointsToEarn(order)).Returns(30);

        var handler = CreateHandler();

        await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        order.PointsToEarn.Should().Be(30);
        loyaltyAccount.PointsBalance.Should().Be(30);
        _loyaltyAccountRepository.Verify(r => r.UpdateAsync(loyaltyAccount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GuestOrder_DoesNotAwardLoyaltyPoints()
    {
        var order = OrderTestFactory.CreateOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        _loyaltyPolicy.Verify(p => p.CalculatePointsToEarn(It.IsAny<Domain.Orders.Order>()), Times.Never);
        _loyaltyAccountRepository.Verify(
            r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ZeroPointsToEarn_DoesNotLoadLoyaltyAccount()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        _loyaltyPolicy.Setup(p => p.CalculatePointsToEarn(order)).Returns(0);

        var handler = CreateHandler();

        await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        order.PointsToEarn.Should().Be(0);
        _loyaltyAccountRepository.Verify(
            r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
