using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Payments.Queries;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Tests.Payments.Queries;

public class GetPaymentStatusQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetPaymentStatusQueryHandler CreateHandler() =>
        new(_orderRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_OwningCustomer_ReturnsPaymentStatus()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId, paymentMethod: PaymentMethod.Online);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetPaymentStatusQuery(order.Id), CancellationToken.None);

        result.OrderId.Should().Be(order.Id);
        result.PaymentMethod.Should().Be(PaymentMethod.Online);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Handle_Staff_ReturnsPaymentStatusForAnyOrder()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.RestaurantAdmin);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetPaymentStatusQuery(order.Id), CancellationToken.None);

        result.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_DifferentCustomer_ThrowsNotFoundException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetPaymentStatusQuery(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetPaymentStatusQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
