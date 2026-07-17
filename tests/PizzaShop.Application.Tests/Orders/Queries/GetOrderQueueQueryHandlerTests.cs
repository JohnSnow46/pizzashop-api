using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Tests.TestHelpers;

namespace PizzaShop.Application.Tests.Orders.Queries;

public class GetOrderQueueQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsQueueOrdersAsDtos()
    {
        var order1 = OrderTestFactory.CreateOrder();
        var order2 = OrderTestFactory.CreateOrder();
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetQueueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { order1, order2 });

        var handler = new GetOrderQueueQueryHandler(repository.Object);

        var result = await handler.Handle(new GetOrderQueueQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(o => o.Id).Should().Contain(new[] { order1.Id, order2.Id });
    }

    [Fact]
    public async Task Handle_EmptyQueue_ReturnsEmptyList()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetQueueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Domain.Orders.Order>());

        var handler = new GetOrderQueueQueryHandler(repository.Object);

        var result = await handler.Handle(new GetOrderQueueQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
