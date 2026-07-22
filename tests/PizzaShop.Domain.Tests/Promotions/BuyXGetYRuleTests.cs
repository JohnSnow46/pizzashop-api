using FluentAssertions;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Domain.Tests.Promotions;

public class BuyXGetYRuleTests
{
    private static readonly Guid TriggerId = Guid.NewGuid();
    private static readonly Guid RewardId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        var rule = new BuyXGetYRule(TriggerId, 2, RewardId, 1, 50m);

        rule.TriggerMenuItemId.Should().Be(TriggerId);
        rule.BuyQuantity.Should().Be(2);
        rule.RewardMenuItemId.Should().Be(RewardId);
        rule.GetQuantity.Should().Be(1);
        rule.RewardDiscountPercentage.Should().Be(50m);
    }

    [Fact]
    public void Constructor_EmptyTriggerMenuItemId_ThrowsArgumentException()
    {
        var act = () => new BuyXGetYRule(Guid.Empty, 2, RewardId, 1, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyRewardMenuItemId_ThrowsArgumentException()
    {
        var act = () => new BuyXGetYRule(TriggerId, 2, Guid.Empty, 1, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_BuyQuantityLessThanOne_ThrowsArgumentOutOfRangeException(int buyQuantity)
    {
        var act = () => new BuyXGetYRule(TriggerId, buyQuantity, RewardId, 1, 100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_GetQuantityLessThanOne_ThrowsArgumentOutOfRangeException(int getQuantity)
    {
        var act = () => new BuyXGetYRule(TriggerId, 2, RewardId, getQuantity, 100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_RewardDiscountPercentageOutOfRange_ThrowsArgumentOutOfRangeException(decimal pct)
    {
        var act = () => new BuyXGetYRule(TriggerId, 2, RewardId, 1, pct);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_RewardDiscountPercentageAt100_DoesNotThrow()
    {
        var act = () => new BuyXGetYRule(TriggerId, 2, RewardId, 1, 100m);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_RewardEqualsTrigger_DoesNotThrow()
    {
        var act = () => new BuyXGetYRule(TriggerId, 2, TriggerId, 1, 100m);

        act.Should().NotThrow();
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new BuyXGetYRule(TriggerId, 2, RewardId, 1, 50m);
        var b = new BuyXGetYRule(TriggerId, 2, RewardId, 1, 50m);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new BuyXGetYRule(TriggerId, 2, RewardId, 1, 50m);
        var b = new BuyXGetYRule(TriggerId, 3, RewardId, 1, 50m);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }
}
