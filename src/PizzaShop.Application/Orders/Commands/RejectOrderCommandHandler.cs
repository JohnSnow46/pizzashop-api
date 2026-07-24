using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Rejects a pending order. Loyalty rollback (ADR-0040): if
/// <c>order.PointsRedeemed &gt; 0</c>, the customer's loyalty account is reversed in the same
/// transaction as the rejection — mirrors <see cref="CancelOrderCommandHandler"/>.
/// </summary>
public sealed class RejectOrderCommandHandler : ICommandHandler<RejectOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _orderNotifier;
    private readonly ILoyaltyAccountRepository _loyaltyAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(RejectOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        order.Reject();

        await ReverseLoyaltyPointsAsync(order, cancellationToken);

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotifier.OrderStatusChangedAsync(order.Id, order.Status, order.EstimatedReadyAt, cancellationToken);

        return Unit.Value;
    }

    /// <summary>Loyalty rollback (ADR-0040) — see <see cref="CancelOrderCommandHandler"/> for the rationale.</summary>
    private async Task ReverseLoyaltyPointsAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.PointsRedeemed <= 0 || order.CustomerId is not { } customerId)
            return;

        var loyaltyAccount = await _loyaltyAccountRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Customer {customerId} redeemed points on order {order.Id} but has no loyalty account on record.");

        loyaltyAccount.Reverse(order.PointsRedeemed, $"Points refunded — order {order.Number} rejected", _clock.UtcNow, order.Id);

        await _loyaltyAccountRepository.UpdateAsync(loyaltyAccount, cancellationToken);
    }
}
