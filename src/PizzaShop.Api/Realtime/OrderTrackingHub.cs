using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PizzaShop.Api.Auth;
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
///
/// <see cref="SubscribeToStaffQueue"/> (ADR-0038 extension) follows the same "gate at
/// subscription time, stay silent on failure" shape, but the check is a role check instead of a
/// query: the class stays <see cref="AllowAnonymousAttribute"/> (guests must still be able to
/// connect for the other two methods), so this method verifies <c>Context.User</c>'s role by
/// hand before joining the <c>"staff"</c> group — there is no attribute equivalent to
/// <c>[Authorize]</c> for a single hub method.
/// </summary>
[AllowAnonymous]
public sealed class OrderTrackingHub : Hub
{
    private const string StaffGroup = "staff";
    private static readonly string[] StaffRoles = AuthRoles.Staff.Split(',');

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

    /// <summary>
    /// Staff order-queue path (ADR-0038 extension): joins the <c>"staff"</c> broadcast group so
    /// <see cref="SignalROrderNotifier.NewOrderPlacedAsync"/> can reach every connected
    /// employee/admin. The hub class is <see cref="AllowAnonymousAttribute"/>, so the role check
    /// happens by hand here, against the same <see cref="AuthRoles.Staff"/> role set (with
    /// hierarchy) other endpoints enforce via <c>[Authorize(Roles = AuthRoles.Staff)]</c>. An
    /// unauthenticated caller or a non-staff role (e.g. Customer) is silently not subscribed — no
    /// exception is thrown to the client, matching the other subscribe methods on this hub.
    /// </summary>
    public Task SubscribeToStaffQueue()
    {
        var user = Context.User;
        var isStaff = user?.Identity?.IsAuthenticated == true && StaffRoles.Any(user.IsInRole);

        return isStaff ? Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup) : Task.CompletedTask;
    }
}
