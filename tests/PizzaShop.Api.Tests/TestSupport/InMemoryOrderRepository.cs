using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IOrderRepository"/> — see <see cref="InMemoryUserAccountRepository"/>
/// for rationale. Tracks the guest-tracking-token and provider-payment-reference sidecars
/// (application-layer.md 3.1) alongside the order itself, exactly as
/// <c>PizzaShop.Infrastructure.Persistence.Repositories.OrderRepository</c> would.
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly ConcurrentDictionary<Guid, Guid> _guestTrackingTokensByOrderId = new();
    private readonly ConcurrentDictionary<Guid, string> _providerPaymentReferencesByOrderId = new();
    private int _orderSequence;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryGetValue(id, out var order) ? order : null);

    public Task<Order?> GetByGuestTrackingTokenAsync(Guid guestTrackingToken, CancellationToken cancellationToken)
    {
        var orderId = _guestTrackingTokensByOrderId
            .Where(kv => kv.Value == guestTrackingToken)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        return Task.FromResult(orderId != Guid.Empty && _orders.TryGetValue(orderId, out var order) ? order : null);
    }

    public Task<IReadOnlyList<Order>> GetQueueAsync(CancellationToken cancellationToken)
    {
        var queue = _orders.Values
            .Where(o => o.Status is not (OrderStatus.Completed or OrderStatus.Rejected or OrderStatus.Cancelled))
            .OrderBy(o => o.PlacedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Order>>(queue);
    }

    public Task AddAsync(Order order, Guid? guestTrackingToken, string? providerPaymentReference, CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;

        if (guestTrackingToken is { } token)
            _guestTrackingTokensByOrderId[order.Id] = token;

        if (providerPaymentReference is not null)
            _providerPaymentReferencesByOrderId[order.Id] = providerPaymentReference;

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<string> NextOrderNumberAsync(CancellationToken cancellationToken) =>
        Task.FromResult($"T-{Interlocked.Increment(ref _orderSequence):D5}");

    public Task SetProviderPaymentReferenceAsync(Guid orderId, string providerPaymentReference, CancellationToken cancellationToken)
    {
        _providerPaymentReferencesByOrderId[orderId] = providerPaymentReference;
        return Task.CompletedTask;
    }

    public Task<string?> GetProviderPaymentReferenceAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(_providerPaymentReferencesByOrderId.TryGetValue(orderId, out var reference) ? reference : null);
}
