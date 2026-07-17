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
    private readonly IUnitOfWork _unitOfWork;

    public MarkReadyCommandHandler(IOrderRepository orderRepository, IOrderNotifier orderNotifier, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _orderNotifier = orderNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(MarkReadyCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        order.MarkReady();

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotifier.OrderStatusChangedAsync(order.Id, order.Status, order.EstimatedReadyAt, cancellationToken);

        return Unit.Value;
    }
}
