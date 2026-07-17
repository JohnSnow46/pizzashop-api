using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Tests.Payments.Commands;

public class ConfirmPaymentFromNotificationCommandHandlerTests
{
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConfirmPaymentFromNotificationCommandHandler CreateHandler() =>
        new(_paymentGateway.Object, _orderRepository.Object, _unitOfWork.Object);

    private static ConfirmPaymentFromNotificationCommand SampleCommand() =>
        new("{}", new Dictionary<string, string> { ["OpenPayu-Signature"] = "abc" });

    private void SetupNotification(Guid orderId, PaymentStatus status) =>
        _paymentGateway
            .Setup(g => g.VerifyAndParseNotification(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new PaymentNotification(orderId, "PAYU-123", status));

    [Fact]
    public async Task Handle_AuthorizedNotificationOnPendingOrder_AuthorizesAndPersists()
    {
        var order = OrderTestFactory.CreateOrder(paymentMethod: PaymentMethod.Online);
        SetupNotification(order.Id, PaymentStatus.Authorized);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(SampleCommand(), CancellationToken.None);

        order.PaymentStatus.Should().Be(PaymentStatus.Authorized);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaidNotificationOnPendingOrder_ConfirmsAndPersists()
    {
        var order = OrderTestFactory.CreateOrder(paymentMethod: PaymentMethod.Online);
        SetupNotification(order.Id, PaymentStatus.Paid);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(SampleCommand(), CancellationToken.None);

        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FailedNotificationOnPendingOrder_FailsAndPersists()
    {
        var order = OrderTestFactory.CreateOrder(paymentMethod: PaymentMethod.Online);
        SetupNotification(order.Id, PaymentStatus.Failed);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(SampleCommand(), CancellationToken.None);

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicatePaidNotificationOnAlreadyPaidOrder_IsIdempotentNoOp()
    {
        var order = OrderTestFactory.CreateOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        SetupNotification(order.Id, PaymentStatus.Paid);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(SampleCommand(), CancellationToken.None);

        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StaleAuthorizedNotificationAfterAlreadyPaid_IsIdempotentNoOp()
    {
        var order = OrderTestFactory.CreateOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        SetupNotification(order.Id, PaymentStatus.Authorized);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        await handler.Handle(SampleCommand(), CancellationToken.None);

        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidSignature_PropagatesInvalidPaymentNotificationException()
    {
        _paymentGateway
            .Setup(g => g.VerifyAndParseNotification(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new InvalidPaymentNotificationException("Invalid notification signature."));

        var handler = CreateHandler();

        var act = () => handler.Handle(SampleCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPaymentNotificationException>();
        _orderRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        var orderId = Guid.NewGuid();
        SetupNotification(orderId, PaymentStatus.Paid);
        _orderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(SampleCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
