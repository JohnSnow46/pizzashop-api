using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Payments.Commands;

public class InitializeGuestPaymentCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private InitializeGuestPaymentCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _paymentGateway.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_KnownTokenOnlineOrder_ReturnsRedirectUrl()
    {
        var order = OrderTestFactory.CreateOrder(customerId: null, paymentMethod: PaymentMethod.Online);
        var token = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByGuestTrackingTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _paymentGateway
            .Setup(g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitResult("https://sandbox.payu.com/pay/789", "PAYU-789"));

        var handler = CreateHandler();

        var result = await handler.Handle(new InitializeGuestPaymentCommand(token), CancellationToken.None);

        result.RedirectUrl.Should().Be("https://sandbox.payu.com/pay/789");
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(
                It.Is<PaymentInitRequest>(r => r.OrderId == order.Id && r.Amount == order.Total),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _orderRepository.Verify(
            r => r.SetProviderPaymentReferenceAsync(order.Id, "PAYU-789", It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByGuestTrackingTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializeGuestPaymentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_OnPickupOrder_ThrowsConflictException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: null, paymentMethod: PaymentMethod.OnPickup);
        var token = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByGuestTrackingTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializeGuestPaymentCommand(token), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyPaidOrder_ThrowsConflictException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: null, paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        var token = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByGuestTrackingTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializeGuestPaymentCommand(token), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
