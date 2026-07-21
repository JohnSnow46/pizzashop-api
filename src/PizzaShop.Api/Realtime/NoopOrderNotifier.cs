using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Realtime;

/// <summary>
/// Temporary <see cref="IOrderNotifier"/> implementation for Iteration 3. Every order
/// status-transition handler (<c>AcceptOrderCommandHandler</c>, <c>CancelOrderCommandHandler</c>,
/// etc., application-layer.md 4.3) already depends on <see cref="IOrderNotifier"/>, but its
/// real implementation — <c>SignalROrderNotifier</c>, backed by <c>OrderTrackingHub</c>
/// (api-layer.md 8, ADR-0028) — is scoped to Iteration 4. Without *some* registration here,
/// every staff status-transition endpoint in <c>OrdersController</c> would fail DI resolution
/// (<see cref="IOrderNotifier"/> has no implementation registered) before Iteration 4 lands.
/// This no-op satisfies the port so Iteration 3 is independently testable/deployable; swapping
/// the DI registration in <c>Program.cs</c> to <c>SignalROrderNotifier</c> is the only change
/// Iteration 4 needs to make here. Flagged to the architect — see builder iteration-3 summary.
/// </summary>
public sealed class NoopOrderNotifier : IOrderNotifier
{
    public Task OrderStatusChangedAsync(
        Guid orderId,
        OrderStatus status,
        DateTimeOffset? estimatedReadyAt,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
