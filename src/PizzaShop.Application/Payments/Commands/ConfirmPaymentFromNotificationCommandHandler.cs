using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Handles <see cref="ConfirmPaymentFromNotificationCommand"/> (application-layer.md 4.4,
/// ADR-0013). A malformed/forged notification never reaches here — an invalid signature
/// makes <see cref="IPaymentGateway.VerifyAndParseNotification"/> throw
/// <see cref="InvalidPaymentNotificationException"/> before this handler does anything, and
/// that exception propagates unhandled (Api middleware maps it to 400/401,
/// application-layer.md 5).
/// </summary>
/// <remarks>
/// <b>Idempotency (the subtle part).</b> PayU may resend a notification (duplicate) or
/// deliver notifications out of order (a delayed "Authorized" arriving after a later "Paid"
/// already landed). Rather than hand-rolling a set of "is this notification stale" checks
/// here, this handler leans on <c>Order</c>'s own payment guard clauses as the single source
/// of truth: it attempts the Domain transition the notified status implies, and if
/// <c>Order</c> throws <see cref="InvalidPaymentStatusTransitionException"/> (i.e. the
/// transition isn't legal from the order's *current* payment state — which covers both "same
/// state again" and "already moved past this state"), that is treated as a harmless no-op,
/// not an error: nothing is persisted and the webhook still reports success. This keeps the
/// transition graph itself (Domain) as the only place idempotency is decided, instead of
/// duplicating it as ad-hoc equality checks in Application.
/// </remarks>
public sealed class ConfirmPaymentFromNotificationCommandHandler : ICommandHandler<ConfirmPaymentFromNotificationCommand>
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmPaymentFromNotificationCommandHandler(
        IPaymentGateway paymentGateway,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentGateway = paymentGateway;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ConfirmPaymentFromNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = _paymentGateway.VerifyAndParseNotification(command.RawBody, command.Headers);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), notification.OrderId);

        if (!TryApplyNotifiedStatus(order, notification.Status))
            return Unit.Value;

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>
    /// Returns <c>true</c> when the notified status was actually applied (a real transition
    /// happened); <c>false</c> when it was a duplicate/stale notification that Domain refused
    /// as an illegal transition — see remarks on the class for why that is a no-op, not an
    /// error.
    /// </summary>
    private static bool TryApplyNotifiedStatus(Order order, PaymentStatus notifiedStatus)
    {
        try
        {
            switch (notifiedStatus)
            {
                case PaymentStatus.Authorized:
                    order.AuthorizePayment();
                    return true;
                case PaymentStatus.Paid:
                    order.ConfirmPayment();
                    return true;
                case PaymentStatus.Failed:
                    order.FailPayment();
                    return true;
                case PaymentStatus.Refunded:
                    order.RefundPayment();
                    return true;
                default:
                    // PaymentStatus.Pending is the initial state only; there is no Domain
                    // transition into it, so a notification claiming it has nothing to apply.
                    return false;
            }
        }
        catch (InvalidPaymentStatusTransitionException)
        {
            return false;
        }
    }
}
