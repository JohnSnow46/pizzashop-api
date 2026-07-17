using FluentAssertions;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Promotions;

public class PromotionTests
{
    private static readonly DateTimeOffset ValidFrom = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidTo = new(2024, 1, 31, 23, 59, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WithinWindow = new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static Promotion CreatePercentagePromotion(
        decimal value = 10m,
        string? code = null,
        Money? minOrderValue = null,
        int? usageLimit = null) =>
        Promotion.Create("10% off", PromotionType.Percentage, ValidFrom, ValidTo, value, code, minOrderValue, usageLimit);

    [Fact]
    public void Create_ValidToBeforeValidFrom_ThrowsArgumentException()
    {
        var act = () => Promotion.Create("Bad window", PromotionType.Percentage, ValidTo, ValidFrom, 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_PercentageValueOutOfRange_ThrowsArgumentOutOfRangeException(decimal value)
    {
        var act = () => Promotion.Create("Bad %", PromotionType.Percentage, ValidFrom, ValidTo, value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_FixedAmountValueZeroOrLess_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Promotion.Create("Bad fixed", PromotionType.FixedAmount, ValidFrom, ValidTo, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_UsageLimitZeroOrLess_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePercentagePromotion(usageLimit: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NewPromotion_IsActiveByDefault()
    {
        var promotion = CreatePercentagePromotion();

        promotion.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsQualifiedFor_Inactive_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion();
        promotion.Deactivate();

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_BeforeValidFrom_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion();

        promotion.IsQualifiedFor(new Money(50m), ValidFrom.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_AfterValidTo_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion();

        promotion.IsQualifiedFor(new Money(50m), ValidTo.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_WithinWindow_ReturnsTrue()
    {
        var promotion = CreatePercentagePromotion();

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeTrue();
    }

    [Fact]
    public void IsQualifiedFor_SubtotalBelowMinOrderValue_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion(minOrderValue: new Money(100m));

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_SubtotalAtOrAboveMinOrderValue_ReturnsTrue()
    {
        var promotion = CreatePercentagePromotion(minOrderValue: new Money(50m));

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeTrue();
    }

    [Fact]
    public void IsQualifiedFor_CodeRequiredButNotSupplied_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion(code: "SUMMER10");

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_CodeMismatch_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion(code: "SUMMER10");

        promotion.IsQualifiedFor(new Money(50m), WithinWindow, "WINTER10").Should().BeFalse();
    }

    [Fact]
    public void IsQualifiedFor_CodeMatchesCaseInsensitively_ReturnsTrue()
    {
        var promotion = CreatePercentagePromotion(code: "SUMMER10");

        promotion.IsQualifiedFor(new Money(50m), WithinWindow, "summer10").Should().BeTrue();
    }

    [Fact]
    public void IsQualifiedFor_UsageLimitReached_ReturnsFalse()
    {
        var promotion = CreatePercentagePromotion(usageLimit: 1);
        promotion.RecordUsage();

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_BeyondLimit_ThrowsPromotionNotApplicableException()
    {
        var promotion = CreatePercentagePromotion(usageLimit: 1);
        promotion.RecordUsage();

        var act = promotion.RecordUsage;

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_Percentage_ComputesPercentageOfSubtotal()
    {
        var promotion = CreatePercentagePromotion(value: 10m);

        var discount = promotion.CalculateDiscount(new Money(100m), new Money(10m), WithinWindow);

        discount.Amount.Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_CapsAtSubtotal()
    {
        var promotion = Promotion.Create("Fixed", PromotionType.FixedAmount, ValidFrom, ValidTo, 50m);

        var discount = promotion.CalculateDiscount(new Money(30m), new Money(10m), WithinWindow);

        discount.Amount.Should().Be(30m);
    }

    [Fact]
    public void CalculateDiscount_FreeDelivery_ReturnsDeliveryFeeAsDiscount()
    {
        var promotion = Promotion.Create("Free delivery", PromotionType.FreeDelivery, ValidFrom, ValidTo);

        var discount = promotion.CalculateDiscount(new Money(30m), new Money(12m), WithinWindow);

        discount.Amount.Should().Be(12m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_ThrowsNotSupportedException()
    {
        var promotion = Promotion.Create("2+1", PromotionType.BuyXGetY, ValidFrom, ValidTo);

        var act = () => promotion.CalculateDiscount(new Money(30m), new Money(10m), WithinWindow);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void CalculateDiscount_NotQualified_ThrowsPromotionNotApplicableException()
    {
        var promotion = CreatePercentagePromotion();
        promotion.Deactivate();

        var act = () => promotion.CalculateDiscount(new Money(30m), new Money(10m), WithinWindow);

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void UpdateWindow_ValidWindow_SetsValidFromAndValidTo()
    {
        var promotion = CreatePercentagePromotion();
        var newFrom = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var newTo = new DateTimeOffset(2024, 2, 28, 23, 59, 0, TimeSpan.Zero);

        promotion.UpdateWindow(newFrom, newTo);

        promotion.ValidFrom.Should().Be(newFrom);
        promotion.ValidTo.Should().Be(newTo);
    }

    [Fact]
    public void UpdateWindow_ValidToBeforeValidFrom_ThrowsArgumentException()
    {
        var promotion = CreatePercentagePromotion();

        var act = () => promotion.UpdateWindow(ValidTo, ValidFrom);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateWindow_ValidToEqualsValidFrom_ThrowsArgumentException()
    {
        var promotion = CreatePercentagePromotion();

        var act = () => promotion.UpdateWindow(ValidFrom, ValidFrom);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateWindow_WindowMovedOutsideCurrentTime_DoesNotThrow_PromotionNoLongerQualifiesGoingForward()
    {
        var promotion = CreatePercentagePromotion();

        promotion.UpdateWindow(ValidTo.AddDays(1), ValidTo.AddDays(30));

        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
    }

    [Fact]
    public void UpdateValue_PercentageWithinRange_SetsValue()
    {
        var promotion = CreatePercentagePromotion(value: 10m);

        promotion.UpdateValue(25m);

        promotion.Value.Should().Be(25m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void UpdateValue_PercentageOutOfRange_ThrowsArgumentOutOfRangeException(decimal value)
    {
        var promotion = CreatePercentagePromotion();

        var act = () => promotion.UpdateValue(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateValue_FixedAmountZeroOrLess_ThrowsArgumentOutOfRangeException()
    {
        var promotion = Promotion.Create("Fixed", PromotionType.FixedAmount, ValidFrom, ValidTo, 50m);

        var act = () => promotion.UpdateValue(0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateValue_AllowedEvenWhenUsageCountGreaterThanZero()
    {
        var promotion = CreatePercentagePromotion(value: 10m);
        promotion.RecordUsage();

        var act = () => promotion.UpdateValue(20m);

        act.Should().NotThrow();
        promotion.Value.Should().Be(20m);
    }

    [Fact]
    public void UpdateUsageLimit_PositiveValue_SetsUsageLimit()
    {
        var promotion = CreatePercentagePromotion();

        promotion.UpdateUsageLimit(5);

        promotion.UsageLimit.Should().Be(5);
    }

    [Fact]
    public void UpdateUsageLimit_Null_ClearsUsageLimit()
    {
        var promotion = CreatePercentagePromotion(usageLimit: 5);

        promotion.UpdateUsageLimit(null);

        promotion.UsageLimit.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateUsageLimit_ZeroOrLess_ThrowsArgumentOutOfRangeException(int usageLimit)
    {
        var promotion = CreatePercentagePromotion();

        var act = () => promotion.UpdateUsageLimit(usageLimit);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateUsageLimit_BelowCurrentUsageCount_DoesNotThrowAndClosesPromotionToNewUsages()
    {
        var promotion = CreatePercentagePromotion(usageLimit: 10);
        promotion.RecordUsage();
        promotion.RecordUsage();
        promotion.RecordUsage();
        promotion.RecordUsage();
        promotion.RecordUsage();

        var act = () => promotion.UpdateUsageLimit(3);

        act.Should().NotThrow();
        promotion.UsageLimit.Should().Be(3);
        promotion.UsageCount.Should().Be(5);
        promotion.IsQualifiedFor(new Money(50m), WithinWindow).Should().BeFalse();
        var act2 = promotion.RecordUsage;
        act2.Should().Throw<PromotionNotApplicableException>();
    }
}
