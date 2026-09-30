using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Carts.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Queries;

public sealed class GetMyCartQueryHandler : IQueryHandler<GetMyCartQuery, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyCartQueryHandler(ICartRepository cartRepository, ICurrentUser currentUser)
    {
        _cartRepository = cartRepository;
        _currentUser = currentUser;
    }

    public async Task<CartDto> Handle(GetMyCartQuery query, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a server-side cart.");

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return CartMapper.ToDto(cart);
    }
}
