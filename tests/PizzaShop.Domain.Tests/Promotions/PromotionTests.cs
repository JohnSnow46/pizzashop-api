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

    private static OrderDiscountContext Context(
        Money subtotal,
        Money deliveryFee,
        DateTimeOffset when,
        string? suppliedCode = null,
        IReadOnlyList<OrderDiscountLine>? lines = null) =>
        new(subtotal, deliveryFee, when, suppliedCode, lines ?? Array.Empty<OrderDiscountLine>());

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

        var discount = promotion.CalculateDiscount(Context(new Money(100m), new Money(10m), WithinWindow));

        discount.Amount.Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_CapsAtSubtotal()
    {
        var promotion = Promotion.Create("Fixed", PromotionType.FixedAmount, ValidFrom, ValidTo, 50m);

        var discount = promotion.CalculateDiscount(Context(new Money(30m), new Money(10m), WithinWindow));

        discount.Amount.Should().Be(30m);
    }

    [Fact]
    public void CalculateDiscount_FreeDelivery_ReturnsDeliveryFeeAsDiscount()
    {
        var promotion = Promotion.Create("Free delivery", PromotionType.FreeDelivery, ValidFrom, ValidTo);

        var discount = promotion.CalculateDiscount(Context(new Money(30m), new Money(12m), WithinWindow));

        discount.Amount.Should().Be(12m);
    }

    [Fact]
    public void CalculateDiscount_NotQualified_ThrowsPromotionNotApplicableException()
    {
        var promotion = CreatePercentagePromotion();
        promotion.Deactivate();

        var act = () => promotion.CalculateDiscount(Context(new Money(30m), new Money(10m), WithinWindow));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    // --- BuyXGetY ---

    private static Promotion CreateBuyXGetYPromotion(BuyXGetYRule rule, string? code = null, int? usageLimit = null) =>
        Promotion.Create("BuyXGetY", PromotionType.BuyXGetY, ValidFrom, ValidTo, null, code, null, usageLimit, rule);

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_ExactSet_DiscountsGetQuantityAtCheapestPrice()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m); // 2+1 free
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var discount = promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(30m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_WithRemainder_DiscountsOnlyFullSets()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m); // 2+1 free
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 4) }; // 1 full set + 1 remainder

        var discount = promotion.CalculateDiscount(Context(new Money(120m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(30m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_MultipleSets_Stacks()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m); // 2+1 free
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 6) }; // 2 full sets

        var discount = promotion.CalculateDiscount(Context(new Money(180m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(60m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_TooFewTriggerUnits_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m); // needs 3 units for a set
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 2) };

        var act = () => promotion.CalculateDiscount(Context(new Money(60m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_MixedPrices_DiscountsCheapestUnitsFirst()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m); // 2+1 free
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine>
        {
            new(pizzaId, new Money(40m), 1),
            new(pizzaId, new Money(20m), 2),
        }; // 3 units total: prices 40, 20, 20 -> cheapest is 20

        var discount = promotion.CalculateDiscount(Context(new Money(80m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(20m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_SameProduct_PartialPercentage_DiscountsProportionally()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 50m); // 2+1 half price
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var discount = promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(15m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_CrossProduct_EnoughRewardUnits_DiscountsGetQuantity()
    {
        var pizzaId = Guid.NewGuid();
        var drinkId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, drinkId, 1, 100m); // buy 2 pizzas, get 1 drink free
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine>
        {
            new(pizzaId, new Money(30m), 2),
            new(drinkId, new Money(8m), 2),
        };

        var discount = promotion.CalculateDiscount(Context(new Money(76m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(8m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_CrossProduct_RewardUnitsLimitDiscount()
    {
        var pizzaId = Guid.NewGuid();
        var drinkId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 1, drinkId, 2, 100m); // buy 1 pizza, get 2 drinks free per set
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine>
        {
            new(pizzaId, new Money(30m), 3), // 3 sets qualify -> up to 6 drinks
            new(drinkId, new Money(8m), 1), // but only 1 drink present
        };

        var discount = promotion.CalculateDiscount(Context(new Money(98m), new Money(10m), WithinWindow, lines: lines));

        discount.Amount.Should().Be(8m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_CrossProduct_NoRewardInCart_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var drinkId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, drinkId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 2) };

        var act = () => promotion.CalculateDiscount(Context(new Money(60m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_CrossProduct_TooFewTriggerUnits_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var drinkId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, drinkId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine>
        {
            new(pizzaId, new Money(30m), 1),
            new(drinkId, new Money(8m), 5),
        };

        var act = () => promotion.CalculateDiscount(Context(new Money(70m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_InactivePromotion_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule);
        promotion.Deactivate();
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var act = () => promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_OutsideWindow_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var act = () => promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), ValidTo.AddDays(1), lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_UsageLimitExhausted_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule, usageLimit: 1);
        promotion.RecordUsage();
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var act = () => promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_WrongCode_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule, code: "PIZZADAY");
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var act = () => promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, "WRONG", lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_BelowMinOrderValue_ThrowsPromotionNotApplicableException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = Promotion.Create(
            "BuyXGetY", PromotionType.BuyXGetY, ValidFrom, ValidTo, null, null, new Money(200m), null, rule);
        var lines = new List<OrderDiscountLine> { new(pizzaId, new Money(30m), 3) };

        var act = () => promotion.CalculateDiscount(Context(new Money(90m), new Money(10m), WithinWindow, lines: lines));

        act.Should().Throw<PromotionNotApplicableException>();
    }

    [Fact]
    public void Create_BuyXGetYWithoutRule_ThrowsArgumentException()
    {
        var act = () => Promotion.Create("BuyXGetY", PromotionType.BuyXGetY, ValidFrom, ValidTo);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BuyXGetYWithValue_ThrowsArgumentException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);

        var act = () => Promotion.Create("BuyXGetY", PromotionType.BuyXGetY, ValidFrom, ValidTo, 10m, null, null, null, rule);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NonBuyXGetYTypeWithRule_ThrowsArgumentException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);

        var act = () => Promotion.Create(
            "10% off", PromotionType.Percentage, ValidFrom, ValidTo, 10m, null, null, null, rule);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateValue_BuyXGetYPromotionWithNonNullValue_ThrowsArgumentException()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = CreateBuyXGetYPromotion(rule);

        var act = () => promotion.UpdateValue(10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BuyXGetYWithRule_SetsBuyXGetYRule()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);

        var promotion = CreateBuyXGetYPromotion(rule);

        promotion.BuyXGetYRule.Should().Be(rule);
        promotion.Value.Should().BeNull();
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
