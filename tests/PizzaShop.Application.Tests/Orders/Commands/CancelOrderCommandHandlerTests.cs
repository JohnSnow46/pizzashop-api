using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Orders.Commands;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderNotifier> _orderNotifier = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private CancelOrderCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _orderNotifier.Object, _paymentGateway.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_OwningCustomerBeforeAccepted_CancelsAndNotifies()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Cancelled, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentCustomer_ThrowsNotFoundException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());

        var handler = CreateHandler();

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Orders.Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StaffCancelsOtherCustomersOrder_Succeeds()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Cancelled, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerCancelsOwnOrderAfterAccepted_ThrowsForbiddenOperationException()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        order.Accept();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
        order.Status.Should().Be(OrderStatus.Accepted);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Orders.Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StaffCancelsOrderAfterAccepted_Succeeds()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        order.Accept();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.RestaurantAdmin);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderNotifier.Verify(
            n => n.OrderStatusChangedAsync(order.Id, OrderStatus.Cancelled, order.EstimatedReadyAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OnlinePaidOrder_RefundsAndPersistsCancelledAndRefunded()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepository
            .Setup(r => r.GetProviderPaymentReferenceAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync("PAYU-123");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaymentStatus.Should().Be(PaymentStatus.Refunded);
        _paymentGateway.Verify(
            g => g.RefundAsync(
                It.Is<PaymentRefundRequest>(r => r.OrderId == order.Id && r.ProviderPaymentReference == "PAYU-123" && r.Amount == order.Total),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnPickupOrder_NeverCallsRefundAsync()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.OnPickup);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _paymentGateway.Verify(
            g => g.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_OnlinePendingPaymentOrder_NeverCallsRefundAsync()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
        _paymentGateway.Verify(
            g => g.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RefundAsyncThrows_NothingIsPersistedAndOrderStaysUncancelled()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepository
            .Setup(r => r.GetProviderPaymentReferenceAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync("PAYU-123");
        _paymentGateway
            .Setup(g => g.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("gateway unreachable"));
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        // Nothing is persisted (repository/unit-of-work never called) — the in-memory
        // aggregate having already transitioned to Cancelled before the gateway call is
        // irrelevant, because it's discarded; no store ever observes this state.
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Orders.Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnlinePaidOrderWithoutStoredReference_ThrowsInvalidOperationException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepository
            .Setup(r => r.GetProviderPaymentReferenceAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        order.Status.Should().Be(OrderStatus.PendingAcceptance);
        _paymentGateway.Verify(
            g => g.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Orders.Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
