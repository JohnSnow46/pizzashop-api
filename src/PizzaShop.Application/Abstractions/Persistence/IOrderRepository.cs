using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Order"/> aggregate (application-layer.md 3.1).
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up a guest order by its unpredictable tracking token (application-layer.md
    /// 4.3.1, flow step 3) — deliberately not <see cref="Order.Id"/>, so a guest's public
    /// tracking link never doubles as the order's primary key (implementation decision,
    /// see <c>CreateOrderCommandHandler</c>).
    /// </summary>
    Task<Order?> GetByGuestTrackingTokenAsync(Guid guestTrackingToken, CancellationToken cancellationToken);

    /// <summary>
    /// Orders still awaiting attention from staff (not yet <c>Completed</c>, <c>Rejected</c>
    /// or <c>Cancelled</c>), oldest first — the incoming queue shown to <c>Employee+</c>.
    /// </summary>
    Task<IReadOnlyList<Order>> GetQueueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a newly created order. <paramref name="guestTrackingToken"/> is the
    /// unpredictable token generated for guest orders (<c>Order.CustomerId is null</c>) and
    /// is <c>null</c> for orders placed by a registered customer, who instead tracks their
    /// own orders via <c>GetOrderByIdQuery</c>. <paramref name="providerPaymentReference"/> is
    /// the payment gateway's own reference for this order (<c>PaymentInitResult.ProviderPaymentReference</c>),
    /// known already at creation time for <see cref="Domain.Enums.PaymentMethod.Online"/>
    /// orders (<c>CreateOrderCommand</c> step 8, ADR-0018); <c>null</c> for
    /// <see cref="Domain.Enums.PaymentMethod.OnPickup"/> orders. Stored as a persistence
    /// sidecar alongside the order, analogous to <paramref name="guestTrackingToken"/> —
    /// Domain never sees it (ADR-0002/ADR-0018).
    /// </summary>
    Task AddAsync(Order order, Guid? guestTrackingToken, string? providerPaymentReference, CancellationToken cancellationToken);

    Task UpdateAsync(Order order, CancellationToken cancellationToken);

    /// <summary>
    /// Generates the next human-readable order number. The actual numbering scheme (e.g. a
    /// daily sequence) is an Infrastructure detail.
    /// </summary>
    Task<string> NextOrderNumberAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets/updates the payment gateway's reference for an existing order — the retry-payment
    /// path (<c>InitializePaymentCommand</c>), where the order already exists but a fresh
    /// gateway session reference must be persisted (ADR-0018). Does not commit itself; the
    /// caller commits via <see cref="PizzaShop.Application.Common.Abstractions.IUnitOfWork"/>.
    /// </summary>
    Task SetProviderPaymentReferenceAsync(Guid orderId, string providerPaymentReference, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the payment gateway's reference for an order — used to issue a refund on
    /// cancellation (<c>CancelOrderCommand</c>, ADR-0018). Returns <c>null</c> if none was ever
    /// recorded (e.g. <see cref="Domain.Enums.PaymentMethod.OnPickup"/> orders).
    /// </summary>
    Task<string?> GetProviderPaymentReferenceAsync(Guid orderId, CancellationToken cancellationToken);
}
