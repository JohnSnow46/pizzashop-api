using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Commands;

/// <summary>
/// Updates name, description/image, price, base ingredients and allowed extras of a
/// <c>MenuItem</c>, and reconciles its variants against <see cref="Variants"/> (full PUT
/// semantics, mirroring <see cref="BaseIngredientIds"/>/<see cref="AllowedExtraIds"/>):
/// entries matched by <see cref="MenuItemVariantInputDto.Id"/> get renamed/repriced and
/// their <c>IsDefault</c> reconciled via <c>MenuItem.SetDefaultVariant</c>, unmatched
/// entries are added as new, and existing variants absent from the list are removed via
/// <c>MenuItem.RemoveVariant</c> (see <c>UpdateMenuItemCommandHandler.ReconcileVariants</c>,
/// ADR-0016). Availability is a separate <see cref="SetMenuItemAvailabilityCommand"/>.
/// </summary>
public sealed record UpdateMenuItemCommand(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    MoneyDto BasePrice,
    IReadOnlyList<Guid> BaseIngredientIds,
    IReadOnlyList<Guid> AllowedExtraIds,
    IReadOnlyList<MenuItemVariantInputDto> Variants) : ICommand;
