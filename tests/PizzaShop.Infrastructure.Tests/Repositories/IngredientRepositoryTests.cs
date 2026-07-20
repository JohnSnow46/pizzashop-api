using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>Round-trip coverage for <see cref="IngredientRepository"/> (the Money conversion in particular).</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class IngredientRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public IngredientRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAndGetById_RoundTripsMoney()
    {
        var ingredient = DomainTestFactory.CreateIngredient("Pepperoni", 4.5m);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new IngredientRepository(writeContext);
            await repository.AddAsync(ingredient, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new IngredientRepository(readContext);

        var loaded = await readRepository.GetByIdAsync(ingredient.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ExtraPrice.Should().Be(ingredient.ExtraPrice);
        loaded.Name.Should().Be(ingredient.Name);
    }
}
