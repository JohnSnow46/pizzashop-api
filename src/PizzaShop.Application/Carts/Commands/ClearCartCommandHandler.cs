using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Commands;

/// <summary>Clearing an already-empty (or nonexistent) cart is a no-op, not an error — unlike
/// item-scoped commands, there is no specific item id whose absence would be surprising.</summary>
public sealed class ClearCartCommandHandler : ICommandHandler<ClearCartCommand>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ClearCartCommandHandler(
        ICartRepository cartRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser, IClock clock)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(ClearCartCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a server-side cart.");

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (cart is null)
            return Unit.Value;

        cart.Clear(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
