using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using PizzaShop.Api.Realtime;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Realtime;

/// <summary>
/// Unit tests for <see cref="OrderTrackingHub"/> (api-layer.md 8.1, ADR-0028). No
/// SignalR-client package is referenced by this test project, and a full
/// WebSocket round-trip isn't needed to verify the hub's subscription logic — so these tests
/// invoke the hub directly with a mocked <see cref="IDispatcher"/>, <see cref="HubCallerContext"/>
/// and <see cref="IGroupManager"/> (both are settable properties on <see cref="Hub"/>, which is
/// how the real SignalR invoker wires them up per call too).
/// </summary>
public sealed class OrderTrackingHubTests
{
    private const string ConnectionId = "connection-1";

    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly Mock<IGroupManager> _groups = new();
    private readonly OrderTrackingHub _hub;

    public OrderTrackingHubTests()
    {
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionId).Returns(ConnectionId);
        context.Setup(c => c.ConnectionAborted).Returns(CancellationToken.None);

        _hub = new OrderTrackingHub(_dispatcher.Object)
        {
            Context = context.Object,
            Groups = _groups.Object,
        };
    }

    private static OrderDto SampleOrder(Guid id) => new(
        id,
        "T-00001",
        null,
        new ContactDetailsDto("Jan Kowalski", "123456789", "jan@example.com"),
        FulfillmentType.Pickup,
        null,
        DateTimeOffset.UtcNow,
        null,
        null,
        OrderStatus.PendingAcceptance,
        PaymentMethod.OnPickup,
        PaymentStatus.Pending,
        new MoneyDto(10m, "PLN"),
        new MoneyDto(0m, "PLN"),
        new MoneyDto(0m, "PLN"),
        new MoneyDto(10m, "PLN"),
        Array.Empty<OrderItemDto>());

    // ---- SubscribeToGuestOrder ----

    [Fact]
    public async Task SubscribeToGuestOrder_ValidToken_AddsConnectionToOrderGroup()
    {
        var orderId = Guid.NewGuid();
        var token = Guid.NewGuid();
        _dispatcher
            .Setup(d => d.Send(It.Is<GetOrderByTrackingTokenQuery>(q => q.GuestTrackingToken == token), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleOrder(orderId));

        await _hub.SubscribeToGuestOrder(token.ToString());

        _groups.Verify(g => g.AddToGroupAsync(ConnectionId, orderId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeToGuestOrder_UnknownToken_DoesNotSubscribe()
    {
        var token = Guid.NewGuid();
        _dispatcher
            .Setup(d => d.Send(It.IsAny<GetOrderByTrackingTokenQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Order", token));

        await _hub.SubscribeToGuestOrder(token.ToString());

        _groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeToGuestOrder_MalformedToken_DoesNotSubscribe()
    {
        await _hub.SubscribeToGuestOrder("not-a-guid");

        _dispatcher.Verify(d => d.Send(It.IsAny<GetOrderByTrackingTokenQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- SubscribeToOrder ----

    [Fact]
    public async Task SubscribeToOrder_OwnedOrder_AddsConnectionToOrderGroup()
    {
        var orderId = Guid.NewGuid();
        _dispatcher
            .Setup(d => d.Send(It.Is<GetOrderByIdQuery>(q => q.OrderId == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleOrder(orderId));

        await _hub.SubscribeToOrder(orderId);

        _groups.Verify(g => g.AddToGroupAsync(ConnectionId, orderId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeToOrder_NotOwnedOrMissing_DoesNotSubscribe()
    {
        var orderId = Guid.NewGuid();
        _dispatcher
            .Setup(d => d.Send(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Order", orderId));

        await _hub.SubscribeToOrder(orderId);

        _groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
