using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Carts;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Carts.Commands;

public sealed class AddCartItemCommandHandler : ICommandHandler<AddCartItemCommand, Guid>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddCartItemCommandHandler(
        ICartRepository cartRepository,
        IMenuItemRepository menuItemRepository,
        IIngredientRepository ingredientRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _cartRepository = cartRepository;
        _menuItemRepository = menuItemRepository;
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have a server-side cart.");

        await EnsureSelectionExistsInCatalogAsync(command, cancellationToken);

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var isNew = cart is null;
        cart ??= Cart.Create(customerId, _clock.UtcNow);

        var itemId = cart.AddItem(command.MenuItemId, command.VariantId, command.ExtraIds, command.Quantity, command.Notes, _clock.UtcNow);

        if (isNew)
            await _cartRepository.AddAsync(cart, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return itemId;
    }

    /// <summary>
    /// Checks that <see cref="AddCartItemCommand.MenuItemId"/>/<see cref="AddCartItemCommand.VariantId"/>/
    /// <see cref="AddCartItemCommand.ExtraIds"/> actually exist in the catalog -- ADR-0043 explicitly left
    /// this unvalidated ("the frontend doesn't do it either today"), relying on <c>CreateOrderCommand</c>
    /// as the only authoritative checkpoint. A cart line referencing a deleted/renamed catalog entry is
    /// harmless today (the frontend already skips it when hydrating, see <c>hydrateFromServerCart</c>), but
    /// it's a cheap, genuine gap to close rather than leave every cart able to silently accumulate dead
    /// references. Deliberately does NOT call <see cref="MenuItem.ResolvePrice"/> -- that also enforces
    /// order-time rules (availability, "variant required if variants exist") that don't apply to a cart,
    /// which ADR-0043 keeps as a draft with no price snapshot.
    /// </summary>
    private async Task EnsureSelectionExistsInCatalogAsync(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(command.MenuItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), command.MenuItemId);

        if (command.VariantId is { } variantId && menuItem.Variants.All(v => v.Id != variantId))
            throw new NotFoundException(nameof(MenuItemVariant), variantId);

        foreach (var extraId in command.ExtraIds.Distinct())
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(extraId, cancellationToken)
                ?? throw new NotFoundException(nameof(Ingredient), extraId);

            menuItem.EnsureExtraAllowed(ingredient);
        }
    }
}
