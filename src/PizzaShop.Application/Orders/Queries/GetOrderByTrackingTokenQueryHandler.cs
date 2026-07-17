using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Orders.Queries;

public sealed class GetOrderByTrackingTokenQueryHandler : IQueryHandler<GetOrderByTrackingTokenQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByTrackingTokenQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByTrackingTokenQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByGuestTrackingTokenAsync(query.GuestTrackingToken, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), query.GuestTrackingToken);

        return OrderMapper.ToDto(order);
    }
}
