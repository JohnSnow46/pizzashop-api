using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class CreateMenuItemCommandHandler : ICommandHandler<CreateMenuItemCommand, Guid>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IIngredientRepository ingredientRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var menuItem = MenuItem.Create(
            command.Name,
            command.Category,
            new Money(command.BasePrice.Amount, command.BasePrice.Currency),
            command.Description,
            command.ImageUrl);

        await AttachIngredients(command.BaseIngredientIds, menuItem.AddBaseIngredient, cancellationToken);
        await AttachIngredients(command.AllowedExtraIds, menuItem.AllowExtra, cancellationToken);

        foreach (var variant in command.Variants)
        {
            menuItem.AddVariant(MenuItemVariant.Create(
                variant.Name,
                new Money(variant.Price.Amount, variant.Price.Currency),
                variant.IsDefault));
        }

        menuItem.EnsureValidCatalogConfiguration();

        await _menuItemRepository.AddAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return menuItem.Id;
    }

    private async Task AttachIngredients(IReadOnlyList<Guid> ids, Action<Ingredient> attach, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return;

        var ingredients = await _ingredientRepository.GetManyByIdsAsync(ids, cancellationToken);
        var missing = ids.Except(ingredients.Select(i => i.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException(nameof(Ingredient), string.Join(", ", missing));

        foreach (var ingredient in ingredients)
            attach(ingredient);
    }
}
