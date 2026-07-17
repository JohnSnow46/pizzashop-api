using FluentAssertions;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Domain.Tests.Loyalty;

public class LoyaltyAccountTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static LoyaltyAccount CreateAccount() => LoyaltyAccount.Create(Guid.NewGuid());

    [Fact]
    public void Create_EmptyCustomerId_ThrowsArgumentException()
    {
        var act = () => LoyaltyAccount.Create(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NewAccount_HasZeroBalanceAndNoTransactions()
    {
        var account = CreateAccount();

        account.PointsBalance.Should().Be(0);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Earn_PositivePoints_IncreasesBalanceAndAppendsTransaction()
    {
        var account = CreateAccount();

        account.Earn(100, "Order #1", Now);

        account.PointsBalance.Should().Be(100);
        account.Transactions.Should().ContainSingle(t => t.Type == LoyaltyTransactionType.Earned && t.Points == 100);
    }

    [Fact]
    public void Earn_NonPositivePoints_ThrowsArgumentOutOfRangeException()
    {
        var account = CreateAccount();

        var act = () => account.Earn(0, "Order #1", Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Redeem_MoreThanBalance_ThrowsInsufficientLoyaltyPointsException()
    {
        var account = CreateAccount();
        account.Earn(50, "Order #1", Now);

        var act = () => account.Redeem(100, "Order #2", Now);

        act.Should().Throw<InsufficientLoyaltyPointsException>();
    }

    [Fact]
    public void Redeem_UpToBalance_DecreasesBalanceAndAppendsNegativeTransaction()
    {
        var account = CreateAccount();
        account.Earn(100, "Order #1", Now);

        account.Redeem(40, "Order #2", Now);

        account.PointsBalance.Should().Be(60);
        account.Transactions.Should().Contain(t => t.Type == LoyaltyTransactionType.Redeemed && t.Points == -40);
    }

    [Fact]
    public void PointsBalance_NeverGoesNegative_AfterFailedRedeem()
    {
        var account = CreateAccount();
        account.Earn(10, "Order #1", Now);

        var act = () => account.Redeem(20, "Order #2", Now);
        act.Should().Throw<InsufficientLoyaltyPointsException>();

        account.PointsBalance.Should().Be(10);
    }

    [Fact]
    public void Adjust_NegativeBeyondBalance_ThrowsInsufficientLoyaltyPointsException()
    {
        var account = CreateAccount();
        account.Earn(10, "Order #1", Now);

        var act = () => account.Adjust(-20, "Correction", Now);

        act.Should().Throw<InsufficientLoyaltyPointsException>();
    }

    [Fact]
    public void Adjust_PositiveValue_IncreasesBalance()
    {
        var account = CreateAccount();

        account.Adjust(15, "Goodwill bonus", Now);

        account.PointsBalance.Should().Be(15);
    }

    [Fact]
    public void Adjust_ZeroPoints_ThrowsArgumentException()
    {
        var account = CreateAccount();

        var act = () => account.Adjust(0, "No-op", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Expire_MoreThanBalance_ThrowsInsufficientLoyaltyPointsException()
    {
        var account = CreateAccount();
        account.Earn(10, "Order #1", Now);

        var act = () => account.Expire(20, "Expired", Now);

        act.Should().Throw<InsufficientLoyaltyPointsException>();
    }

    [Fact]
    public void Expire_UpToBalance_DecreasesBalance()
    {
        var account = CreateAccount();
        account.Earn(50, "Order #1", Now);

        account.Expire(20, "Yearly expiry", Now);

        account.PointsBalance.Should().Be(30);
    }

    [Fact]
    public void Transactions_AreAppendOnly_PreviousTransactionsRemainUnchanged()
    {
        var account = CreateAccount();

        account.Earn(50, "Order #1", Now);
        account.Redeem(20, "Order #2", Now.AddDays(1));

        account.Transactions.Should().HaveCount(2);
        var first = account.Transactions.First();
        first.Points.Should().Be(50);
        first.Type.Should().Be(LoyaltyTransactionType.Earned);
    }

    [Fact]
    public void PointsBalance_AlwaysEqualsSumOfTransactions()
    {
        var account = CreateAccount();

        account.Earn(100, "Order #1", Now);
        account.Redeem(30, "Order #2", Now);
        account.Adjust(-10, "Correction", Now);
        account.Expire(5, "Expiry", Now);

        account.PointsBalance.Should().Be(account.Transactions.Sum(t => t.Points));
    }
}
