using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class UpdateMenuItemCommandHandler : ICommandHandler<UpdateMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IIngredientRepository ingredientRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), command.Id);

        menuItem.Rename(command.Name);
        menuItem.UpdateDetails(command.Description, command.ImageUrl);
        menuItem.UpdateBasePrice(new Money(command.BasePrice.Amount, command.BasePrice.Currency));

        await ReconcileIngredients(
            command.BaseIngredientIds,
            menuItem.BaseIngredients.Select(i => i.Id).ToList(),
            menuItem.AddBaseIngredient,
            menuItem.RemoveBaseIngredient,
            cancellationToken);

        await ReconcileIngredients(
            command.AllowedExtraIds,
            menuItem.AllowedExtras.Select(i => i.Id).ToList(),
            menuItem.AllowExtra,
            menuItem.DisallowExtra,
            cancellationToken);

        ReconcileVariants(menuItem, command.Variants);

        menuItem.EnsureValidCatalogConfiguration();

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task ReconcileIngredients(
        IReadOnlyList<Guid> targetIds,
        IReadOnlyList<Guid> currentIds,
        Action<Ingredient> add,
        Action<Guid> remove,
        CancellationToken cancellationToken)
    {
        foreach (var idToRemove in currentIds.Except(targetIds))
            remove(idToRemove);

        var idsToAdd = targetIds.Except(currentIds).ToList();
        if (idsToAdd.Count == 0)
            return;

        var ingredients = await _ingredientRepository.GetManyByIdsAsync(idsToAdd, cancellationToken);
        var missing = idsToAdd.Except(ingredients.Select(i => i.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException(nameof(Ingredient), string.Join(", ", missing));

        foreach (var ingredient in ingredients)
            add(ingredient);
    }

    /// <summary>
    /// Reconciles <c>menuItem.Variants</c> against the DTO (PUT semantics, mirroring
    /// <see cref="ReconcileIngredients"/>): existing entries are renamed/repriced, new
    /// entries are added, and existing variants absent from the DTO are removed. Default
    /// reassignment runs before removal — Domain requires <c>SetDefaultVariant(new)</c>
    /// before <c>RemoveVariant(old)</c> when a removal targets the current default
    /// (domain-model.md 4.4, ADR-0016).
    /// </summary>
    private static void ReconcileVariants(MenuItem menuItem, IReadOnlyList<MenuItemVariantInputDto> variants)
    {
        var targetExistingIds = variants
            .Where(v => v.Id.HasValue)
            .Select(v => v.Id!.Value)
            .ToHashSet();

        var idsToRemove = menuItem.Variants
            .Select(v => v.Id)
            .Where(id => !targetExistingIds.Contains(id))
            .ToList();

        foreach (var input in variants)
        {
            if (input.Id is { } existingId)
            {
                if (menuItem.Variants.All(v => v.Id != existingId))
                    throw new NotFoundException(nameof(MenuItemVariant), existingId);

                menuItem.RenameVariant(existingId, input.Name);
                menuItem.UpdateVariantPrice(existingId, new Money(input.Price.Amount, input.Price.Currency));
            }
            else
            {
                menuItem.AddVariant(MenuItemVariant.Create(
                    input.Name,
                    new Money(input.Price.Amount, input.Price.Currency),
                    input.IsDefault));
            }
        }

        var requestedDefaultId = variants
            .Where(v => v.Id.HasValue && v.IsDefault)
            .Select(v => v.Id!.Value)
            .FirstOrDefault(Guid.Empty);

        if (requestedDefaultId != Guid.Empty)
            menuItem.SetDefaultVariant(requestedDefaultId);

        foreach (var idToRemove in idsToRemove)
            menuItem.RemoveVariant(idToRemove);
    }
}
