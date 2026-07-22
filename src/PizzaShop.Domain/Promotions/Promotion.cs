using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Promotions;

/// <summary>
/// Promotion aggregate — an elastic sketch (domain-model.md 8): qualification is a fixed
/// set of checks (active, validity window, minimum order value, coupon code, usage
/// limit), but the exact catalogue of discount rules is expected to grow via a future ADR.
/// One promotion per order on start (<c>Order.AppliedPromotionId</c>); stacking is a
/// future decision.
/// </summary>
public class Promotion
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Code { get; private set; }
    public PromotionType Type { get; }
    public decimal? Value { get; private set; }
    public BuyXGetYRule? BuyXGetYRule { get; private set; }
    public Money? MinOrderValue { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Promotion()
    {
    }

    private Promotion(
        Guid id,
        string name,
        PromotionType type,
        decimal? value,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        string? code,
        Money? minOrderValue,
        int? usageLimit,
        BuyXGetYRule? buyXGetYRule)
    {
        Id = id;
        Name = name;
        Type = type;
        Value = value;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Code = code;
        MinOrderValue = minOrderValue;
        UsageLimit = usageLimit;
        IsActive = true;
        UsageCount = 0;
        BuyXGetYRule = buyXGetYRule;
    }

    public static Promotion Create(
        string name,
        PromotionType type,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        decimal? value = null,
        string? code = null,
        Money? minOrderValue = null,
        int? usageLimit = null,
        BuyXGetYRule? buyXGetYRule = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (validTo <= validFrom)
            throw new ArgumentException("Valid-to must be after valid-from.", nameof(validTo));
        if (usageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be greater than zero when set.");

        ValidateValue(type, value);
        ValidateBuyXGetYRule(type, buyXGetYRule);

        return new Promotion(Guid.NewGuid(), name, type, value, validFrom, validTo, NormalizeCode(code), minOrderValue, usageLimit, buyXGetYRule);
    }

    private static void ValidateValue(PromotionType type, decimal? value)
    {
        switch (type)
        {
            case PromotionType.Percentage when value is not (> 0 and <= 100):
                throw new ArgumentOutOfRangeException(nameof(value), "Percentage value must be greater than 0 and at most 100.");
            case PromotionType.FixedAmount when value is not > 0:
                throw new ArgumentOutOfRangeException(nameof(value), "Fixed amount value must be greater than zero.");
            case PromotionType.BuyXGetY when value is not null:
                // Keeps the "Value is null for BuyXGetY" invariant true not just at Create
                // time but also across UpdateValue (domain-model.md 8.2, ADR-0034) —
                // configuration lives exclusively in BuyXGetYRule.
                throw new ArgumentException("BuyXGetY promotions must not set Value; configuration lives in BuyXGetYRule.", nameof(value));
        }
    }

    /// <summary>
    /// Guards the coupling between <see cref="Type"/> and <see cref="BuyXGetYRule"/>
    /// (domain-model.md 8.2, ADR-0034): a <c>BuyXGetY</c> promotion requires a rule; every
    /// other type must not carry one. <see cref="Value"/> being null for <c>BuyXGetY</c> is
    /// already enforced by <see cref="ValidateValue"/>, called before this.
    /// </summary>
    private static void ValidateBuyXGetYRule(PromotionType type, BuyXGetYRule? buyXGetYRule)
    {
        if (type == PromotionType.BuyXGetY)
        {
            if (buyXGetYRule is null)
                throw new ArgumentException("BuyXGetY promotions require a BuyXGetYRule.", nameof(buyXGetYRule));
        }
        else if (buyXGetYRule is not null)
        {
            throw new ArgumentException($"BuyXGetYRule is only allowed for {nameof(PromotionType.BuyXGetY)} promotions.", nameof(buyXGetYRule));
        }
    }

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Replaces the validity window (domain-model.md 8.1, ADR-0019). Both ends are set
    /// together because they are coupled by the <c>ValidTo &gt; ValidFrom</c> invariant.
    /// Not coupled to <see cref="UsageCount"/> — the window may be moved freely, even so
    /// that "now" falls outside it (already-recorded usages are snapshotted on <c>Order</c>
    /// and are unaffected).
    /// </summary>
    public void UpdateWindow(DateTimeOffset validFrom, DateTimeOffset validTo)
    {
        if (validTo <= validFrom)
            throw new ArgumentException("Valid-to must be after valid-from.", nameof(validTo));

        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    /// <summary>
    /// Replaces the discount value, applying the same type-dependent validation as
    /// <see cref="Create"/>. Allowed regardless of <see cref="UsageCount"/> — already placed
    /// orders snapshot their discount amount, so this only affects future applications
    /// (domain-model.md 8.1, ADR-0019).
    /// </summary>
    public void UpdateValue(decimal? value)
    {
        ValidateValue(Type, value);

        Value = value;
    }

    /// <summary>
    /// Replaces the global usage limit (<c>null</c> = unlimited). Setting it below the
    /// current <see cref="UsageCount"/> is deliberately allowed — it immediately closes the
    /// promotion to new usages via <see cref="IsQualifiedFor"/>/<see cref="RecordUsage"/>,
    /// without violating any invariant (domain-model.md 8.1, ADR-0019).
    /// </summary>
    public void UpdateUsageLimit(int? usageLimit)
    {
        if (usageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be greater than zero when set.");

        UsageLimit = usageLimit;
    }

    /// <summary>
    /// Whether an order with the given subtotal, placed at <paramref name="when"/> and
    /// (optionally) supplying a coupon code, qualifies for this promotion
    /// (domain-model.md 8, "Reguły (zarys)").
    /// </summary>
    public bool IsQualifiedFor(Money subtotal, DateTimeOffset when, string? suppliedCode = null)
    {
        ArgumentNullException.ThrowIfNull(subtotal);

        if (!IsActive)
            return false;
        if (when < ValidFrom || when > ValidTo)
            return false;
        if (MinOrderValue is { } minimum && subtotal < minimum)
            return false;
        if (UsageLimit is { } limit && UsageCount >= limit)
            return false;
        if (Code is not null && !string.Equals(Code, NormalizeCode(suppliedCode), StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>
    /// Calculates the discount amount for an order, given its full discount context (subtotal,
    /// delivery fee, timing/code, and line items — domain-model.md 8.2, ADR-0034). Callers are
    /// expected to have checked <see cref="IsQualifiedFor"/> first; this re-checks it and throws
    /// <see cref="PromotionNotApplicableException"/> if it has not (or no longer) qualified.
    /// <c>BuyXGetY</c> additionally requires at least one full trigger set and at least one
    /// reward unit present in <paramref name="ctx"/>'s <see cref="OrderDiscountContext.Lines"/>
    /// — otherwise the same exception is thrown (8.2).
    /// </summary>
    public Money CalculateDiscount(OrderDiscountContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (!IsQualifiedFor(ctx.Subtotal, ctx.When, ctx.SuppliedCode))
            throw new PromotionNotApplicableException($"Promotion '{Name}' is not applicable to this order.");

        return Type switch
        {
            PromotionType.Percentage => new Money(Math.Round(ctx.Subtotal.Amount * Value!.Value / 100m, 2), ctx.Subtotal.Currency),
            PromotionType.FixedAmount => new Money(Math.Min(Value!.Value, ctx.Subtotal.Amount), ctx.Subtotal.Currency),
            PromotionType.FreeDelivery => ctx.DeliveryFee,
            PromotionType.BuyXGetY => CalculateBuyXGetYDiscount(ctx),
            _ => throw new NotSupportedException($"Unknown promotion type '{Type}'."),
        };
    }

    /// <summary>
    /// BuyXGetY discount calculation (domain-model.md 8.2, ADR-0034). Counts trigger units
    /// across <paramref name="ctx"/>'s lines, derives how many full "sets" qualify (floor
    /// division — same-product sets are sized <c>X+Y</c>, cross-product sets are sized <c>X</c>
    /// and capped by the reward units actually present), then discounts the cheapest reward
    /// units by <see cref="Domain.Promotions.BuyXGetYRule.RewardDiscountPercentage"/>.
    /// </summary>
    private Money CalculateBuyXGetYDiscount(OrderDiscountContext ctx)
    {
        var rule = BuyXGetYRule!;

        var triggerUnits = ctx.Lines.Where(l => l.MenuItemId == rule.TriggerMenuItemId).Sum(l => l.Quantity);
        var rewardLines = ctx.Lines.Where(l => l.MenuItemId == rule.RewardMenuItemId).ToList();

        int discountedUnits;
        if (rule.RewardMenuItemId == rule.TriggerMenuItemId)
        {
            var setSize = rule.BuyQuantity + rule.GetQuantity;
            var sets = triggerUnits / setSize;
            discountedUnits = sets * rule.GetQuantity;
        }
        else
        {
            var sets = triggerUnits / rule.BuyQuantity;
            var rewardUnits = rewardLines.Sum(l => l.Quantity);
            discountedUnits = Math.Min(sets * rule.GetQuantity, rewardUnits);
        }

        if (discountedUnits <= 0)
            throw new PromotionNotApplicableException($"Promotion '{Name}' is not applicable to this order.");

        var discountedUnitPrices = rewardLines
            .OrderBy(l => l.UnitPrice.Amount)
            .SelectMany(l => Enumerable.Repeat(l.UnitPrice, l.Quantity))
            .Take(discountedUnits);

        var currency = ctx.Subtotal.Currency;
        return discountedUnitPrices.Aggregate(
            Money.Zero(currency),
            (total, unitPrice) => total.Add(new Money(Math.Round(unitPrice.Amount * rule.RewardDiscountPercentage / 100m, 2), currency)));
    }

    /// <summary>Marks a single redemption against <see cref="UsageLimit"/>, if one is set.</summary>
    public void RecordUsage()
    {
        if (UsageLimit is { } limit && UsageCount >= limit)
            throw new PromotionNotApplicableException($"Promotion '{Name}' has reached its usage limit of {limit}.");

        UsageCount++;
    }
}
