using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Promotions.Commands;

/// <summary>
/// Updates a promotion (RestaurantAdmin, application-layer.md 4.5): activation/deactivation,
/// validity window, discount value, and usage limit — via the dedicated
/// <c>Promotion.UpdateWindow</c>/<c>UpdateValue</c>/<c>UpdateUsageLimit</c> methods
/// (domain-model.md 8.1, ADR-0019).
///
/// <para>
/// <see cref="ValidFrom"/>/<see cref="ValidTo"/> are only applied when both are supplied —
/// <c>Promotion.UpdateWindow</c> sets both ends of the window together because they are
/// coupled by the <c>ValidTo &gt; ValidFrom</c> invariant. Supplying only one of the pair is
/// rejected by <c>UpdatePromotionCommandValidator</c> (ambiguous shape); both absent leaves
/// the window unchanged.
/// </para>
/// <para>
/// <see cref="Value"/> and <see cref="UsageLimit"/> are applied only when supplied —
/// <c>null</c>/absent leaves them unchanged. This means this command cannot explicitly clear
/// an existing <see cref="UsageLimit"/> back to "unlimited" via this shape; that gap is
/// accepted for now since application-layer.md 4.5 does not require it — the operational path
/// to close a promotion is either lowering <see cref="UsageLimit"/> or <see cref="IsActive"/>
/// = false.
/// </para>
/// </summary>
public sealed record UpdatePromotionCommand(
    Guid PromotionId,
    bool IsActive,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    decimal? Value = null,
    int? UsageLimit = null) : ICommand;
