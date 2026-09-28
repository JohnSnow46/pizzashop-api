using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Reviews.Commands;

/// <summary>
/// Rates (and optionally comments on) one menu item from one of the caller's own,
/// completed orders (Customer role, ADR-0042). No customer id on the command — the handler
/// reads it from <c>ICurrentUser</c>, same pattern as <c>CancelOrderCommandHandler</c>: a
/// customer can only ever review as themselves, never on someone else's behalf.
/// </summary>
public sealed record CreateReviewCommand(
    Guid OrderId,
    Guid MenuItemId,
    int Rating,
    string? Comment) : ICommand<Guid>;
