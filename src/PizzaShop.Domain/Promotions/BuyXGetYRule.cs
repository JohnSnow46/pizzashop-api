namespace PizzaShop.Domain.Promotions;

/// <summary>
/// Configuration for a <c>PromotionType.BuyXGetY</c> promotion (owned VO on <see cref="Promotion"/>,
/// present iff <c>Type == BuyXGetY</c>; domain-model.md 8.2, ADR-0034). Defines a concrete
/// trigger product/quantity and a concrete reward product/quantity/discount — reward may be the
/// same product as the trigger (classic "N for M") or a different one (buy a pizza, get a drink
/// cheaper). Immutable after creation, same as <see cref="Promotion.Type"/> — changing the rule
/// is a new promotion.
/// </summary>
public sealed class BuyXGetYRule : IEquatable<BuyXGetYRule>
{
    public Guid TriggerMenuItemId { get; }
    public int BuyQuantity { get; }
    public Guid RewardMenuItemId { get; }
    public int GetQuantity { get; }
    public decimal RewardDiscountPercentage { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private BuyXGetYRule()
    {
    }

    public BuyXGetYRule(
        Guid triggerMenuItemId,
        int buyQuantity,
        Guid rewardMenuItemId,
        int getQuantity,
        decimal rewardDiscountPercentage)
    {
        if (triggerMenuItemId == Guid.Empty)
            throw new ArgumentException("Trigger menu item id is required.", nameof(triggerMenuItemId));
        if (buyQuantity < 1)
            throw new ArgumentOutOfRangeException(nameof(buyQuantity), "Buy quantity must be at least 1.");
        if (rewardMenuItemId == Guid.Empty)
            throw new ArgumentException("Reward menu item id is required.", nameof(rewardMenuItemId));
        if (getQuantity < 1)
            throw new ArgumentOutOfRangeException(nameof(getQuantity), "Get quantity must be at least 1.");
        if (rewardDiscountPercentage is not (> 0 and <= 100))
            throw new ArgumentOutOfRangeException(nameof(rewardDiscountPercentage), "Reward discount percentage must be greater than 0 and at most 100.");

        TriggerMenuItemId = triggerMenuItemId;
        BuyQuantity = buyQuantity;
        RewardMenuItemId = rewardMenuItemId;
        GetQuantity = getQuantity;
        RewardDiscountPercentage = rewardDiscountPercentage;
    }

    public bool Equals(BuyXGetYRule? other) =>
        other is not null
        && TriggerMenuItemId == other.TriggerMenuItemId
        && BuyQuantity == other.BuyQuantity
        && RewardMenuItemId == other.RewardMenuItemId
        && GetQuantity == other.GetQuantity
        && RewardDiscountPercentage == other.RewardDiscountPercentage;

    public override bool Equals(object? obj) => Equals(obj as BuyXGetYRule);

    public override int GetHashCode() =>
        HashCode.Combine(TriggerMenuItemId, BuyQuantity, RewardMenuItemId, GetQuantity, RewardDiscountPercentage);

    public static bool operator ==(BuyXGetYRule? left, BuyXGetYRule? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BuyXGetYRule? left, BuyXGetYRule? right) => !(left == right);
}
