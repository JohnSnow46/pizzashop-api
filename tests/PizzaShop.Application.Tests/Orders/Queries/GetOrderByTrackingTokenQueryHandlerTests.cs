using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Orders.Queries;

public class GetOrderByTrackingTokenQueryHandlerTests
{
    [Fact]
    public async Task Handle_KnownToken_ReturnsDto()
    {
        var order = OrderTestFactory.CreateOrder();
        var token = Guid.NewGuid();
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetByGuestTrackingTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new GetOrderByTrackingTokenQueryHandler(repository.Object);

        var result = await handler.Handle(new GetOrderByTrackingTokenQuery(token), CancellationToken.None);

        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsNotFoundException()
    {
        var repository = new Mock<IOrderRepository>();
        repository
            .Setup(r => r.GetByGuestTrackingTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Orders.Order?)null);

        var handler = new GetOrderByTrackingTokenQueryHandler(repository.Object);

        var act = () => handler.Handle(new GetOrderByTrackingTokenQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
