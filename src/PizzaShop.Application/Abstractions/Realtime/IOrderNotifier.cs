using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Abstractions.Realtime;

/// <summary>
/// Live-tracking port for order status updates (SignalR implementation lives in Api,
/// application-layer.md 3.2). Called after every successful <c>OrderStatus</c> transition
/// (and whenever <c>EstimatedReadyAt</c> changes), so the customer's tracking view updates
/// in real time.
/// </summary>
public interface IOrderNotifier
{
    Task OrderStatusChangedAsync(
        Guid orderId,
        OrderStatus status,
        DateTimeOffset? estimatedReadyAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called once after a new order is persisted (<c>CreateOrderCommandHandler</c>), so the
    /// staff order queue can pick it up without a manual refresh. Broadcast to the "staff" group
    /// only — <c>OrderTrackingHub</c> gates membership in that group by role at subscription
    /// time, not here.
    /// </summary>
    Task NewOrderPlacedAsync(Guid orderId, CancellationToken cancellationToken);
}
