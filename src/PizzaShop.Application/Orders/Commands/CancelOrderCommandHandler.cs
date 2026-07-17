using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Cancels an order. Ownership is scoped exactly like
/// <see cref="Queries.GetOrderByIdQueryHandler"/> (application-layer.md 4.3): staff
/// (<see cref="UserRole.Employee"/>/<see cref="UserRole.RestaurantAdmin"/>/<see cref="UserRole.SuperAdmin"/>)
/// can cancel any order, a customer only their own — a non-owning customer gets the same
/// <see cref="NotFoundException"/> as a non-existent order id, so the endpoint never
/// confirms whether an order belongs to someone else.
/// A customer (unlike staff) may additionally only cancel while the order is still
/// <see cref="OrderStatus.PendingAcceptance"/>; once staff have accepted it, only staff can
/// cancel. <c>Order.Cancel()</c> itself only rejects terminal states and doesn't know about
/// roles, so this extra restriction is enforced here.
/// Refund orchestration (application-layer.md 4.3.3, ADR-0018): a paid online order
/// (<see cref="Domain.Enums.PaymentMethod.Online"/> + <see cref="Domain.Enums.PaymentStatus.Paid"/>)
/// is refunded through <see cref="IPaymentGateway.RefundAsync"/> before the cancellation is
/// persisted, so a failed refund leaves nothing saved (the order stays in its previous state,
/// ready to retry).
/// </summary>
public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _orderNotifier;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        var isStaff = EnsureAccessAllowed(order);
        EnsureCustomerCanStillCancel(order, isStaff);

        var mustRefund = order.PaymentMethod == PaymentMethod.Online && order.PaymentStatus == PaymentStatus.Paid;

        string? providerPaymentReference = null;
        if (mustRefund)
        {
            providerPaymentReference = await _orderRepository.GetProviderPaymentReferenceAsync(order.Id, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Order {order.Id} is paid online but has no provider payment reference on record.");
        }

        order.Cancel();

        if (mustRefund)
        {
            await _paymentGateway.RefundAsync(
                new PaymentRefundRequest(order.Id, providerPaymentReference!, order.Total),
                cancellationToken);

            order.RefundPayment();
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotifier.OrderStatusChangedAsync(order.Id, order.Status, order.EstimatedReadyAt, cancellationToken);

        return Unit.Value;
    }

    private bool EnsureAccessAllowed(Order order)
    {
        var isStaff = _currentUser.Role is UserRole.Employee or UserRole.RestaurantAdmin or UserRole.SuperAdmin;
        if (isStaff)
            return true;

        if (order.CustomerId is null || order.CustomerId != _currentUser.CustomerId)
            throw new NotFoundException(nameof(Order), order.Id);

        return false;
    }

    private static void EnsureCustomerCanStillCancel(Order order, bool isStaff)
    {
        if (isStaff || order.Status == OrderStatus.PendingAcceptance)
            return;

        throw new ForbiddenOperationException(
            "Customers can only cancel an order before it has been accepted by the restaurant.");
    }
}
