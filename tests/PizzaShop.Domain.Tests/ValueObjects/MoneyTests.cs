using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new Money(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ZeroAmount_DoesNotThrow()
    {
        var act = () => new Money(0m);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NoCurrencySpecified_DefaultsToPln()
    {
        var money = new Money(10m);

        money.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var result = new Money(10m).Add(new Money(5m));

        result.Amount.Should().Be(15m);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsInvalidOperationException()
    {
        var act = () => new Money(10m, "PLN").Add(new Money(5m, "EUR"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_SubtractsAmounts()
    {
        var result = new Money(10m).Subtract(new Money(4m));

        result.Amount.Should().Be(6m);
    }

    [Fact]
    public void Multiply_PositiveQuantity_MultipliesAmount()
    {
        var result = new Money(3m).Multiply(4);

        result.Amount.Should().Be(12m);
    }

    [Fact]
    public void Multiply_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new Money(3m).Multiply(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        var a = new Money(10m);
        var b = new Money(10m);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        var a = new Money(10m);
        var b = new Money(20m);

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void ComparisonOperators_OrderByAmount_BehaveAsExpected()
    {
        var small = new Money(5m);
        var big = new Money(10m);
        var sameAsSmall = new Money(5m);
        var sameAsBig = new Money(10m);

        (small < big).Should().BeTrue();
        (big > small).Should().BeTrue();
        (small <= sameAsSmall).Should().BeTrue();
        (big >= sameAsBig).Should().BeTrue();
    }

    [Fact]
    public void Zero_ReturnsAmountZeroWithGivenCurrency()
    {
        var zero = Money.Zero("EUR");

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EUR");
    }
}
