using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Handles <see cref="InitializePaymentCommand"/>. Ownership is scoped exactly like
/// <see cref="Orders.Queries.GetOrderByIdQueryHandler"/>/<see cref="Orders.Commands.CancelOrderCommandHandler"/>:
/// staff can act on any order, a customer only their own; a non-owning customer gets the
/// same <see cref="NotFoundException"/> as a non-existent order id. Guest orders (no
/// <c>CustomerId</c>) are not reachable through this ownership check — retrying payment as a
/// guest would need a tracking-token-scoped variant analogous to
/// <see cref="Orders.Queries.GetOrderByTrackingTokenQueryHandler"/>, which is out of scope for
/// this iteration (flagged for a follow-up, not needed for the inline path inside
/// <c>CreateOrderCommand</c>, which does not go through this command).
/// </summary>
public sealed class InitializePaymentCommandHandler : ICommandHandler<InitializePaymentCommand, InitializePaymentResultDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public InitializePaymentCommandHandler(
        IOrderRepository orderRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<InitializePaymentResultDto> Handle(InitializePaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        EnsureAccessAllowed(order);
        EnsureCanInitializePayment(order);

        var result = await _paymentGateway.InitializePaymentAsync(
            new PaymentInitRequest(order.Id, order.Number, order.Total, order.Contact.Email, $"PizzaShop order {order.Number}"),
            cancellationToken);

        await _orderRepository.SetProviderPaymentReferenceAsync(order.Id, result.ProviderPaymentReference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new InitializePaymentResultDto(result.RedirectUrl);
    }

    private void EnsureAccessAllowed(Order order)
    {
        var isStaff = _currentUser.Role is UserRole.Employee or UserRole.RestaurantAdmin or UserRole.SuperAdmin;
        if (isStaff)
            return;

        if (order.CustomerId is null || order.CustomerId != _currentUser.CustomerId)
            throw new NotFoundException(nameof(Order), order.Id);
    }

    /// <summary>
    /// Guards a case Domain itself has no method for (initializing a gateway session isn't an
    /// <c>Order</c> state transition) and is universal rather than role-dependent — a state
    /// conflict illegal for every caller, signalled with <see cref="ConflictException"/> (409,
    /// ADR-0018), not <see cref="ForbiddenOperationException"/> (which is reserved for
    /// role-dependent denials, ADR-0017).
    /// </summary>
    private static void EnsureCanInitializePayment(Order order)
    {
        if (order.PaymentMethod != PaymentMethod.Online)
            throw new ConflictException("Payment can only be initialized for orders placed with online payment.");

        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new ConflictException("This order has already been paid.");
    }
}
