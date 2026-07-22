using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Round-trip coverage for <see cref="LoyaltyAccountRepository"/> — the append-only owned
/// <c>Transactions</c> history and the resulting <c>PointsBalance</c> (ADR-0009).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LoyaltyAccountRepositoryTests : PostgresRepositoryTestBase
{
    public LoyaltyAccountRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAndGet_RoundTripsTransactionHistoryAndBalance()
    {
        var customerId = Guid.NewGuid();
        var account = DomainTestFactory.CreateLoyaltyAccountWithHistory(customerId);

        await using (var writeContext = Fixture.CreateContext())
        {
            var repository = new LoyaltyAccountRepository(writeContext);
            await repository.AddAsync(account, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new LoyaltyAccountRepository(readContext);

        var loaded = await readRepository.GetByCustomerIdAsync(customerId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.PointsBalance.Should().Be(account.PointsBalance);
        loaded.Transactions.Should().HaveCount(2);
        loaded.Transactions.Select(t => t.Points).Should().BeEquivalentTo(account.Transactions.Select(t => t.Points));
    }
}
