using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Queries;

/// <summary>
/// Returns an order by id for its owning customer or staff (application-layer.md 4.3).
/// Ownership is enforced here (not in Api) because it depends on the loaded order's
/// <c>CustomerId</c>, which Api's role policy cannot know in advance — a non-owning
/// customer gets the same <see cref="NotFoundException"/> as a non-existent order, so the
/// endpoint never confirms whether an order id belongs to someone else.
/// </summary>
public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), query.OrderId);

        EnsureAccessAllowed(order);

        return OrderMapper.ToDto(order);
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
