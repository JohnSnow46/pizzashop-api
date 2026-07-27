using Microsoft.Extensions.Logging;
using PizzaShop.Application.Abstractions.Email;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Realtime;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Commands;

public sealed class MarkReadyCommandHandler : ICommandHandler<MarkReadyCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _orderNotifier;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkReadyCommandHandler> _logger;

    public MarkReadyCommandHandler(
        IOrderRepository orderRepository,
        IOrderNotifier orderNotifier,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        ILogger<MarkReadyCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(MarkReadyCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        order.MarkReady();

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
}
