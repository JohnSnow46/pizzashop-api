using Microsoft.AspNetCore.SignalR;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Realtime;

/// <summary>
/// <see cref="IOrderNotifier"/> implementation (api-layer.md 8.2, ADR-0028), backed by
/// <see cref="IHubContext{OrderTrackingHub}"/>. Pushes to the <c>OrderId</c>-keyed SignalR
/// group only — who is actually listening in that group (guest via token, customer, staff) was
/// already decided at subscription time in <see cref="OrderTrackingHub"/>, so this port stays
/// keyed by <c>OrderId</c> alone, matching the Application-layer contract.
/// </summary>
public sealed class SignalROrderNotifier : IOrderNotifier
{
    private readonly IHubContext<OrderTrackingHub> _hub;

    public SignalROrderNotifier(IHubContext<OrderTrackingHub> hub)
    {
        _hub = hub;
    }

    public Task OrderStatusChangedAsync(
        Guid orderId,
        OrderStatus status,
        DateTimeOffset? estimatedReadyAt,
        CancellationToken cancellationToken)
    {
        var payload = new { orderId, status, estimatedReadyAt };
        return _hub.Clients.Group(orderId.ToString()).SendAsync("OrderStatusChanged", payload, cancellationToken);
    }

    /// <summary>
    /// Broadcasts to the <c>"staff"</c> group only (membership gated by role in
    /// <see cref="OrderTrackingHub.SubscribeToStaffQueue"/>). Minimal payload — the front end
    /// re-fetches order details through the existing queue/order queries instead of duplicating
    /// that lookup here.
    /// </summary>
    public Task NewOrderPlacedAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payload = new { orderId };
        return _hub.Clients.Group("staff").SendAsync("NewOrderPlaced", payload, cancellationToken);
    }
}
