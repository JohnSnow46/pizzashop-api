using FluentAssertions;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Domain.Tests.Loyalty;

/// <summary>
/// <c>LoyaltyTransaction.Create</c> is internal and only reachable through
/// <c>LoyaltyAccount</c> (domain-model.md 7.2: "append-only", "encje tylko rejestrują
/// skutki") — so its guard clauses are exercised via the owning aggregate.
/// </summary>
public class LoyaltyTransactionTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Earn_BlankReason_ThrowsArgumentException()
    {
        var account = LoyaltyAccount.Create(Guid.NewGuid());

        var act = () => account.Earn(10, "  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Earn_WithOrderId_RecordsOrderIdOnTransaction()
    {
        var account = LoyaltyAccount.Create(Guid.NewGuid());
        var orderId = Guid.NewGuid();

        account.Earn(10, "Order reward", Now, orderId);

        account.Transactions.Single().OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Create_Transaction_HasOccurredAtSetToProvidedInstant()
    {
        var account = LoyaltyAccount.Create(Guid.NewGuid());

        account.Earn(10, "Order reward", Now);

        account.Transactions.Single().OccurredAt.Should().Be(Now);
    }
}
