using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Queries;

namespace PizzaShop.Api.Realtime;

/// <summary>
/// Live-tracking hub (api-layer.md 8.1, ADR-0028), mapped to <c>/hubs/order-tracking</c> in
/// Program.cs. <see cref="AllowAnonymousAttribute"/> — guests must be able to subscribe without
/// a JWT (ADR-0005). Groups are keyed by <c>OrderId</c> (not by the guest tracking token), so
/// <see cref="SignalROrderNotifier"/> stays keyed by <c>OrderId</c> only and both guest and
/// logged-in subscribers of the same order land in one group.
///
/// Authorization happens at subscription time, not at push time: each subscribe method resolves
/// the caller's access through the same <see cref="IDispatcher"/> queries the REST endpoints use
/// (<see cref="GetOrderByTrackingTokenQuery"/>/<see cref="GetOrderByIdQuery"/>), and only joins
/// the group on success. A failed lookup (bad/unknown token, not-owned order) silently subscribes
/// nobody — the hub never reveals whether an order or token exists.
/// </summary>
[AllowAnonymous]
public sealed class OrderTrackingHub : Hub
{
    private readonly IDispatcher _dispatcher;

    public OrderTrackingHub(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Guest tracking path (api-layer.md 6.6 <c>GET /track/{trackingToken}</c> equivalent for
    /// SignalR). Possession of the unguessable token is the access control — an invalid/unknown
    /// token silently subscribes nobody instead of surfacing an error to the caller.
    /// </summary>
    public async Task SubscribeToGuestOrder(string trackingToken)
    {
        if (!Guid.TryParse(trackingToken, out var token))
            return;

        try
        {
            var order = await _dispatcher.Send(new GetOrderByTrackingTokenQuery(token), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, order.Id.ToString());
        }
        catch (NotFoundException)
        {
            // Unknown/invalid token — stay silent, don't confirm or deny an order exists.
        }
        catch (ValidationException)
        {
            // Malformed query (e.g. empty token) — stay silent, same reasoning as above.
        }
    }

    /// <summary>
    /// Logged-in path. Ownership vs. staff scoping happens inside
    /// <c>GetOrderByIdQueryHandler</c> (reads <c>ICurrentUser</c> off the same JWT the hub
    /// connection carries — <c>access_token</c> query string, Program.cs), so a non-owning
    /// customer gets the same silent non-subscription as a nonexistent order.
    /// </summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        try
        {
            var order = await _dispatcher.Send(new GetOrderByIdQuery(orderId), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, order.Id.ToString());
        }
        catch (NotFoundException)
        {
            // Not found / not owned by the caller — stay silent (same reasoning as above).
        }
        catch (ValidationException)
        {
            // Malformed query (e.g. empty orderId) — stay silent, same reasoning as above.
        }
    }
}
