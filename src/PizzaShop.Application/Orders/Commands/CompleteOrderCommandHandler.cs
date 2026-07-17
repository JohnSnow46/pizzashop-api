using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Completes an order and, for a registered customer (application-layer.md 4.6, ADR-0009),
/// awards loyalty points via <see cref="ILoyaltyPolicy.CalculatePointsToEarn"/> →
/// <c>Order.SetPointsToEarn</c> → <c>LoyaltyAccount.Earn</c>. Guest orders (no
/// <c>CustomerId</c>) never earn points (ADR-0005).
/// </summary>
public sealed class CompleteOrderCommandHandler : ICommandHandler<CompleteOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _orderNotifier;
    private readonly ILoyaltyAccountRepository _loyaltyAccountRepository;
    private readonly ILoyaltyPolicy _loyaltyPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompleteOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        ILoyaltyPolicy loyaltyPolicy,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _loyaltyPolicy = loyaltyPolicy;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(CompleteOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        order.Complete();

        if (order.CustomerId is { } customerId)
            await AwardLoyaltyPointsAsync(order, customerId, cancellationToken);

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotifier.OrderStatusChangedAsync(order.Id, order.Status, order.EstimatedReadyAt, cancellationToken);

        return Unit.Value;
    }

    private async Task AwardLoyaltyPointsAsync(Order order, Guid customerId, CancellationToken cancellationToken)
    {
        var pointsToEarn = _loyaltyPolicy.CalculatePointsToEarn(order);
        order.SetPointsToEarn(pointsToEarn);

        if (pointsToEarn <= 0)
            return;

        var loyaltyAccount = await _loyaltyAccountRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LoyaltyAccount), customerId);

        loyaltyAccount.Earn(pointsToEarn, $"Order {order.Number} completed", _clock.UtcNow, order.Id);

        await _loyaltyAccountRepository.UpdateAsync(loyaltyAccount, cancellationToken);
    }
}
