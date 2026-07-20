using FluentAssertions;
using PizzaShop.Domain.ValueObjects;
using PizzaShop.Infrastructure.Persistence.Converters;

namespace PizzaShop.Infrastructure.Tests.Persistence;

/// <summary>Pure unit tests for <see cref="MoneyConverter"/> — no database required.</summary>
public sealed class MoneyConverterTests
{
    private readonly MoneyConverter _converter = new();

    [Fact]
    public void ConvertToProvider_ReturnsAmount()
    {
        var money = new Money(12.5m);

        var stored = (decimal?)_converter.ConvertToProvider(money);

        stored.Should().Be(12.5m);
    }

    [Fact]
    public void ConvertFromProvider_ReturnsMoneyInDefaultCurrency()
    {
        var restored = (Money?)_converter.ConvertFromProvider(12.5m);

        restored!.Amount.Should().Be(12.5m);
        restored.Currency.Should().Be(Money.DefaultCurrency);
    }

    [Fact]
    public void RoundTrip_PreservesAmount()
    {
        var original = new Money(99.99m);

        var stored = _converter.ConvertToProvider(original);
        var roundTripped = (Money?)_converter.ConvertFromProvider(stored);

        roundTripped.Should().Be(original);
    }
}
