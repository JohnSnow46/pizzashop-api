using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>Round-trip coverage for <see cref="PromotionRepository"/> (ADR-0019).</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PromotionRepositoryTests : PostgresRepositoryTestBase
{
    public PromotionRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAndGetByCode_RoundTripsPromotion()
    {
        var promotion = DomainTestFactory.CreatePromotion();

        await using (var writeContext = Fixture.CreateContext())
        {
            var repository = new PromotionRepository(writeContext);
            await repository.AddAsync(promotion, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new PromotionRepository(readContext);

        var loaded = await readRepository.GetByCodeAsync("summer10", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(promotion.Id);
        loaded.Value.Should().Be(promotion.Value);
        loaded.MinOrderValue.Should().Be(promotion.MinOrderValue);
    }
}
