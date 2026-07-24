using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>. <c>GuestTrackingToken</c> and
/// <c>ProviderPaymentReference</c> are persisted as shadow properties on the <c>Orders</c>
/// table (ADR-0021) — accessed here through the change tracker/<see cref="EF.Property{T}"/>,
/// never exposed on the <see cref="Order"/> class itself.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private const string GuestTrackingTokenProperty = "GuestTrackingToken";
    private const string ProviderPaymentReferenceProperty = "ProviderPaymentReference";

    private static readonly OrderStatus[] TerminalStatuses =
    {
        OrderStatus.Completed,
        OrderStatus.Rejected,
        OrderStatus.Cancelled,
    };

    private readonly PizzaShopDbContext _context;
    private readonly IClock _clock;

    public OrderRepository(PizzaShopDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByGuestTrackingTokenAsync(Guid guestTrackingToken, CancellationToken cancellationToken) =>
        _context.Orders.FirstOrDefaultAsync(
            o => EF.Property<Guid?>(o, GuestTrackingTokenProperty) == guestTrackingToken,
            cancellationToken);

    public async Task<IReadOnlyList<Order>> GetQueueAsync(CancellationToken cancellationToken) =>
        await _context.Orders
            .Where(o => !TerminalStatuses.Contains(o.Status))
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        Order order,
        Guid? guestTrackingToken,
        string? providerPaymentReference,
        CancellationToken cancellationToken)
    {
        await _context.Orders.AddAsync(order, cancellationToken);

        var entry = _context.Entry(order);
        entry.Property(GuestTrackingTokenProperty).CurrentValue = guestTrackingToken;
        entry.Property(ProviderPaymentReferenceProperty).CurrentValue = providerPaymentReference;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken)
    {
        // EF Core's SqlQueryRaw<T> for a scalar T requires the result set's single column to be
        // named "Value" — Postgres otherwise names it "nextval" after the function, which EF's
        // generated wrapping query then fails to find.
        var next = await _context.Database
            .SqlQueryRaw<long>($"SELECT nextval('{PizzaShopDbContext.OrderNumberSequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"{_clock.UtcNow:yyyyMMdd}-{next:D4}";
    }

    public async Task SetProviderPaymentReferenceAsync(
        Guid orderId,
        string providerPaymentReference,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), orderId);

        _context.Entry(order).Property(ProviderPaymentReferenceProperty).CurrentValue = providerPaymentReference;
    }

    public Task<string?> GetProviderPaymentReferenceAsync(Guid orderId, CancellationToken cancellationToken) =>
        _context.Orders
            .Where(o => o.Id == orderId)
            .Select(o => EF.Property<string?>(o, ProviderPaymentReferenceProperty))
            .FirstOrDefaultAsync(cancellationToken);
}
