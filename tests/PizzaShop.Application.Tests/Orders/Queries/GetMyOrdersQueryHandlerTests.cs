using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Orders.Queries;

public class GetMyOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetMyOrdersQueryHandler CreateHandler() =>
        new(_orderRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_LoggedInCustomer_ReturnsOwnOrdersMappedToSummaries()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        _orderRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { order });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        var summary = result[0];
        summary.Id.Should().Be(order.Id);
        summary.Number.Should().Be(order.Number);
        summary.PlacedAt.Should().Be(order.PlacedAt);
        summary.Status.Should().Be(order.Status);
        summary.FulfillmentType.Should().Be(order.FulfillmentType);
        summary.PaymentStatus.Should().Be(order.PaymentStatus);
        summary.Total.Amount.Should().Be(order.Total.Amount);
        summary.Total.Currency.Should().Be(order.Total.Currency);
        summary.ItemsCount.Should().Be(order.Items.Count);
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _orderRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Domain.Orders.Order>());

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyOrdersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
