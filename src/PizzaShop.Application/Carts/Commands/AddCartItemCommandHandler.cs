using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Carts.Commands;

public sealed class AddCartItemCommandHandler : ICommandHandler<AddCartItemCommand, Guid>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser, IClock clock)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a server-side cart.");

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var isNew = cart is null;
        cart ??= Cart.Create(customerId, _clock.UtcNow);

        var itemId = cart.AddItem(command.MenuItemId, command.VariantId, command.ExtraIds, command.Quantity, command.Notes, _clock.UtcNow);

        if (isNew)
            await _cartRepository.AddAsync(cart, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return itemId;
    }
}
