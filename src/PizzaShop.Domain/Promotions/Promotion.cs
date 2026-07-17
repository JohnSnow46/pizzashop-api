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
    public Money? MinOrderValue { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }

    private Promotion(
        Guid id,
        string name,
        PromotionType type,
        decimal? value,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        string? code,
        Money? minOrderValue,
        int? usageLimit)
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
    }

    public static Promotion Create(
        string name,
        PromotionType type,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        decimal? value = null,
        string? code = null,
        Money? minOrderValue = null,
        int? usageLimit = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (validTo <= validFrom)
            throw new ArgumentException("Valid-to must be after valid-from.", nameof(validTo));
        if (usageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be greater than zero when set.");

        ValidateValue(type, value);

        return new Promotion(Guid.NewGuid(), name, type, value, validFrom, validTo, NormalizeCode(code), minOrderValue, usageLimit);
    }

    private static void ValidateValue(PromotionType type, decimal? value)
    {
        switch (type)
        {
            case PromotionType.Percentage when value is not (> 0 and <= 100):
                throw new ArgumentOutOfRangeException(nameof(value), "Percentage value must be greater than 0 and at most 100.");
            case PromotionType.FixedAmount when value is not > 0:
                throw new ArgumentOutOfRangeException(nameof(value), "Fixed amount value must be greater than zero.");
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
    /// Calculates the discount amount for an order's subtotal/delivery fee. Callers are
    /// expected to have checked <see cref="IsQualifiedFor"/> first; this throws
    /// <see cref="PromotionNotApplicableException"/> if it has not (or no longer)
    /// qualified. <c>BuyXGetY</c> needs the order's line items to resolve which item is
    /// free, which this aggregate does not have — its calculation is deferred to a future
    /// ADR (domain-model.md 8).
    /// </summary>
    public Money CalculateDiscount(Money subtotal, Money deliveryFee, DateTimeOffset when, string? suppliedCode = null)
    {
        ArgumentNullException.ThrowIfNull(deliveryFee);

        if (!IsQualifiedFor(subtotal, when, suppliedCode))
            throw new PromotionNotApplicableException($"Promotion '{Name}' is not applicable to this order.");

        return Type switch
        {
            PromotionType.Percentage => new Money(Math.Round(subtotal.Amount * Value!.Value / 100m, 2), subtotal.Currency),
            PromotionType.FixedAmount => new Money(Math.Min(Value!.Value, subtotal.Amount), subtotal.Currency),
            PromotionType.FreeDelivery => deliveryFee,
            PromotionType.BuyXGetY => throw new NotSupportedException(
                $"Promotion '{Name}': BuyXGetY discount calculation depends on order line items and is not yet defined (domain-model.md 8, deferred to a future ADR)."),
            _ => throw new NotSupportedException($"Unknown promotion type '{Type}'."),
        };
    }

    /// <summary>Marks a single redemption against <see cref="UsageLimit"/>, if one is set.</summary>
    public void RecordUsage()
    {
        if (UsageLimit is { } limit && UsageCount >= limit)
            throw new PromotionNotApplicableException($"Promotion '{Name}' has reached its usage limit of {limit}.");

        UsageCount++;
    }
}
