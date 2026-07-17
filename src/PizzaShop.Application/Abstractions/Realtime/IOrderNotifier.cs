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
}
