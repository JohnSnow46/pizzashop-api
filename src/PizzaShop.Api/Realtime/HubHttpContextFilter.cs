using Microsoft.AspNetCore.SignalR;

namespace PizzaShop.Api.Realtime;

/// <summary>
/// Re-anchors <see cref="IHttpContextAccessor.HttpContext"/> to the current hub invocation's own
/// <c>HttpContext</c> for the duration of that invocation (api-layer.md 8.1, ADR-0028).
///
/// <see cref="IHttpContextAccessor"/> is <see cref="AsyncLocal{T}"/>-backed, and ASP.NET Core's
/// hosting pipeline clears that <c>AsyncLocal</c> holder's <c>HttpContext</c> when the specific
/// HTTP request that delivered a hub invocation finishes (e.g. the individual long-polling POST
/// carrying a <c>SubscribeToOrder</c> message) — even while the hub method's own async
/// continuation is still executing past that point. Confirmed by instrumenting
/// <see cref="PizzaShop.Api.Auth.HttpContextCurrentUser"/>: an <c>await</c> inside
/// <c>GetOrderByIdQueryHandler</c> resumed with <c>IHttpContextAccessor.HttpContext == null</c>,
/// even though <see cref="HubCallerContext.User"/> and <c>HubCallerContext.GetHttpContext()</c>
/// were still correctly authenticated at that same point. Any Application-layer code resolving
/// <c>ICurrentUser</c> after that silently loses the caller's identity — invisible in an ordinary
/// request/response flow (the accessor is normally cleared only once the whole request is truly
/// done), but very real over SignalR, where a hub method routinely keeps running past the
/// lifetime of the individual transport request that triggered it.
///
/// Registered via <c>AddSignalR(options => options.AddFilter&lt;HubHttpContextFilter&gt;())</c>
/// in Program.cs — applies to every hub method, not just <c>OrderTrackingHub</c>'s current ones.
/// </summary>
public sealed class HubHttpContextFilter : IHubFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HubHttpContextFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var previousHttpContext = _httpContextAccessor.HttpContext;

        // Assigning through the setter creates a fresh AsyncLocal holder for this invocation's
        // own call chain, so subsequent awaits within it keep seeing this HttpContext even after
        // an unrelated request (the one that physically delivered the message) finishes and clears
        // its own holder.
        _httpContextAccessor.HttpContext = invocationContext.Context.GetHttpContext();
        try
        {
            return await next(invocationContext);
        }
        finally
        {
            _httpContextAccessor.HttpContext = previousHttpContext;
        }
    }
}
