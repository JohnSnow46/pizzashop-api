using Microsoft.Extensions.Logging;
using PizzaShop.Application.Abstractions.Email;
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
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RejectOrderCommandHandler> _logger;

    public RejectOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RejectOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
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
        await NotifyCustomerByEmailAsync(order, cancellationToken);

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
}
