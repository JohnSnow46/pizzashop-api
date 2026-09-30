using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Commands;

public sealed class RemoveCartItemCommandHandler : ICommandHandler<RemoveCartItemCommand>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RemoveCartItemCommandHandler(
        ICartRepository cartRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser, IClock clock)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(RemoveCartItemCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a server-side cart.");

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Cart", customerId);

        try
        {
            cart.RemoveItem(command.CartItemId, _clock.UtcNow);
        }
        catch (ArgumentException)
        {
            throw new NotFoundException("CartItem", command.CartItemId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
