using Microsoft.Extensions.Logging;
using PizzaShop.Application.Abstractions.Email;
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
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<CompleteOrderCommandHandler> _logger;

    public CompleteOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        ILoyaltyPolicy loyaltyPolicy,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<CompleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _loyaltyPolicy = loyaltyPolicy;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
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
        await NotifyCustomerByEmailAsync(order, cancellationToken);

        return Unit.Value;
    }

    private async Task NotifyCustomerByEmailAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            Guid? guestTrackingToken = order.CustomerId is null
                ? await _orderRepository.GetGuestTrackingTokenAsync(order.Id, cancellationToken)
                : null;

            await _emailSender.SendOrderStatusChangedEmailAsync(
                order.Contact.Email, order.Number, order.Id, order.Status, guestTrackingToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send status-changed email for order {OrderId}.", order.Id);
        }
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
