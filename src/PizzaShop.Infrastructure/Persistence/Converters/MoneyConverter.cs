using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="Money"/> as a single <c>numeric(12,2)</c> column — currency is
/// implied PLN in this single-currency scope, so a second "currency" column would be dead
/// weight (ADR-0020, infrastructure-layer.md 2.2). Registered globally in
/// <see cref="PizzaShopDbContext.ConfigureConventions"/>, not per-property.
/// </summary>
public sealed class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            amount => new Money(amount, Money.DefaultCurrency))
    {
    }
}
