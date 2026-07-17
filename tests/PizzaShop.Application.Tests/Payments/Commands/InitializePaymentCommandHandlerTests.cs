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

public class InitializePaymentCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private InitializePaymentCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _paymentGateway.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_OwningCustomerOnlineOrder_ReturnsRedirectUrl()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId, paymentMethod: PaymentMethod.Online);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _paymentGateway
            .Setup(g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitResult("https://sandbox.payu.com/pay/123", "PAYU-123"));

        var handler = CreateHandler();

        var result = await handler.Handle(new InitializePaymentCommand(order.Id), CancellationToken.None);

        result.RedirectUrl.Should().Be("https://sandbox.payu.com/pay/123");
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(
                It.Is<PaymentInitRequest>(r => r.OrderId == order.Id && r.Amount == order.Total),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _orderRepository.Verify(
            r => r.SetProviderPaymentReferenceAsync(order.Id, "PAYU-123", It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StaffInitializesAnyOrder_ReturnsRedirectUrl()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);
        _paymentGateway
            .Setup(g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitResult("https://sandbox.payu.com/pay/456", "PAYU-456"));

        var handler = CreateHandler();

        var result = await handler.Handle(new InitializePaymentCommand(order.Id), CancellationToken.None);

        result.RedirectUrl.Should().Be("https://sandbox.payu.com/pay/456");
    }

    [Fact]
    public async Task Handle_DifferentCustomer_ThrowsNotFoundException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializePaymentCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializePaymentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OnPickupOrder_ThrowsConflictException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.OnPickup);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializePaymentCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyPaidOrder_ThrowsConflictException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid(), paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new InitializePaymentCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
