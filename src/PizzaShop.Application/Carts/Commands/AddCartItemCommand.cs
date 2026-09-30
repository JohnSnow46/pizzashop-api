using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Carts.Commands;

/// <summary>
/// Adds an item to the caller's own cart, or merges into an existing line with the same
/// selection (ADR-0043 §B). No customer id on the command — read from <c>ICurrentUser</c>.
/// Returns the id of the resulting cart line (new or merged).
/// </summary>
public sealed record AddCartItemCommand(
    Guid MenuItemId, Guid? VariantId, IReadOnlyList<Guid> ExtraIds, int Quantity, string? Notes) : ICommand<Guid>;
