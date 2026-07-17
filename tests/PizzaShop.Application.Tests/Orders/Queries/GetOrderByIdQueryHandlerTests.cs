using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Orders.Queries;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetOrderByIdQueryHandler CreateHandler() => new(_orderRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_OwningCustomer_ReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestFactory.CreateOrder(customerId: customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_StaffRole_ReturnsDtoRegardlessOfOwnership()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_DifferentCustomer_ThrowsNotFoundException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GuestOrderAccessedByCustomer_ThrowsNotFoundException()
    {
        var order = OrderTestFactory.CreateOrder(customerId: null);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Customer);
        _currentUser.Setup(c => c.CustomerId).Returns(Guid.NewGuid());

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Orders.Order?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
