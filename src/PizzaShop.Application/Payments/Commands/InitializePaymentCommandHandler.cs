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
/// <c>CustomerId</c>) are not reachable through this ownership check — the tracking-token-scoped
/// equivalent for guests is <see cref="InitializeGuestPaymentCommandHandler"/> (ADR-0041), which
/// shares <see cref="PaymentInitializationGuard"/> with this handler.
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
        PaymentInitializationGuard.EnsureCanInitializePayment(order);

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
}
