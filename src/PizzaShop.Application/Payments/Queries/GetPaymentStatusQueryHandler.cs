using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Payments.Queries;

/// <summary>
/// Handles <see cref="GetPaymentStatusQuery"/>. Ownership scoped exactly like
/// <see cref="Orders.Queries.GetOrderByIdQueryHandler"/>: staff may look up any order, a
/// customer only their own; a non-owning customer gets the same <see cref="NotFoundException"/>
/// as a non-existent order id.
/// </summary>
public sealed class GetPaymentStatusQueryHandler : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public GetPaymentStatusQueryHandler(IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), query.OrderId);

        EnsureAccessAllowed(order);

        return new PaymentStatusDto(order.Id, order.PaymentMethod, order.PaymentStatus);
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
