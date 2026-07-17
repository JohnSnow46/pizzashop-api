using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Catalog.Dtos;

public sealed record MenuItemVariantDto(Guid Id, string Name, MoneyDto Price, bool IsDefault);

/// <summary>
/// Input shape for creating/updating a variant. <see cref="Id"/> is null for a brand-new
/// variant and set to match an existing one when updating its price
/// (see <c>UpdateMenuItemCommandHandler</c>).
/// </summary>
public sealed record MenuItemVariantInputDto(Guid? Id, string Name, MoneyDto Price, bool IsDefault);
