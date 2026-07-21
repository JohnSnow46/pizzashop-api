using Microsoft.AspNetCore.SignalR;
using Moq;
using PizzaShop.Api.Realtime;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Realtime;

/// <summary>
/// Unit test for <see cref="SignalROrderNotifier"/> (api-layer.md 8.2, ADR-0028): verifies it
/// pushes the <c>OrderStatusChanged</c> payload to the <c>OrderId</c>-keyed SignalR group via
/// a mocked <see cref="IHubContext{OrderTrackingHub}"/>.
/// </summary>
public sealed class SignalROrderNotifierTests
{
    [Fact]
    public async Task OrderStatusChangedAsync_SendsPayloadToOrderGroup()
    {
        var orderId = Guid.NewGuid();
        var estimatedReadyAt = DateTimeOffset.UtcNow.AddMinutes(20);

        var clientProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(orderId.ToString())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<OrderTrackingHub>>();
        hubContext.Setup(h => h.Clients).Returns(clients.Object);

        var notifier = new SignalROrderNotifier(hubContext.Object);

        await notifier.OrderStatusChangedAsync(orderId, OrderStatus.Accepted, estimatedReadyAt, CancellationToken.None);

        clientProxy.Verify(
            p => p.SendCoreAsync(
                "OrderStatusChanged",
                It.Is<object?[]>(args => args.Length == 1 && PayloadMatches(args[0]!, orderId, OrderStatus.Accepted, estimatedReadyAt)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static bool PayloadMatches(object payload, Guid orderId, OrderStatus status, DateTimeOffset? estimatedReadyAt)
    {
        var type = payload.GetType();
        var actualOrderId = (Guid)type.GetProperty("orderId")!.GetValue(payload)!;
        var actualStatus = (OrderStatus)type.GetProperty("status")!.GetValue(payload)!;
        var actualEstimatedReadyAt = (DateTimeOffset?)type.GetProperty("estimatedReadyAt")!.GetValue(payload);

        return actualOrderId == orderId && actualStatus == status && actualEstimatedReadyAt == estimatedReadyAt;
    }
}
