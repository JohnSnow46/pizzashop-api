using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Handles <see cref="InitializeGuestPaymentCommand"/>. Lookup mirrors
/// <see cref="Orders.Queries.GetOrderByTrackingTokenQueryHandler"/> — no ownership check beyond
/// the token itself (ADR-0041). The rest of the flow is identical to
/// <see cref="InitializePaymentCommandHandler"/>, sharing <see cref="PaymentInitializationGuard"/>.
/// </summary>
public sealed class InitializeGuestPaymentCommandHandler : ICommandHandler<InitializeGuestPaymentCommand, InitializePaymentResultDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;

    public InitializeGuestPaymentCommandHandler(
        IOrderRepository orderRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<InitializePaymentResultDto> Handle(InitializeGuestPaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByGuestTrackingTokenAsync(command.GuestTrackingToken, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.GuestTrackingToken);

        PaymentInitializationGuard.EnsureCanInitializePayment(order);

        var result = await _paymentGateway.InitializePaymentAsync(
            new PaymentInitRequest(order.Id, order.Number, order.Total, order.Contact.Email, $"PizzaShop order {order.Number}"),
            cancellationToken);

        await _orderRepository.SetProviderPaymentReferenceAsync(order.Id, result.ProviderPaymentReference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new InitializePaymentResultDto(result.RedirectUrl);
    }
}
