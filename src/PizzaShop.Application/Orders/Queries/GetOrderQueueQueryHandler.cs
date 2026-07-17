using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;

namespace PizzaShop.Application.Orders.Queries;

public sealed class GetOrderQueueQueryHandler : IQueryHandler<GetOrderQueueQuery, IReadOnlyList<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueueQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrderQueueQuery query, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetQueueAsync(cancellationToken);
        return orders.Select(OrderMapper.ToDto).ToList();
    }
}
