using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Round-trip coverage for <see cref="MenuItemRepository"/> — owned <c>Variants</c> plus the
/// two independent many-to-many relations to the shared <c>Ingredient</c> dictionary
/// (<c>BaseIngredients</c>/<c>AllowedExtras</c>), the hardest part of the catalog mapping
/// (ADR-0020).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MenuItemRepositoryTests : PostgresRepositoryTestBase
{
    public MenuItemRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAndGet_RoundTripsVariantsAndBothIngredientRelations()
    {
        var baseIngredient = DomainTestFactory.CreateIngredient("Mozzarella");
        var extraIngredient = DomainTestFactory.CreateIngredient("Mushrooms", 3m);
        var menuItem = DomainTestFactory.CreatePizzaWithVariantsAndIngredients(baseIngredient, extraIngredient);

        await using (var writeContext = Fixture.CreateContext())
        {
            var ingredientRepository = new IngredientRepository(writeContext);
            await ingredientRepository.AddAsync(baseIngredient, CancellationToken.None);
            await ingredientRepository.AddAsync(extraIngredient, CancellationToken.None);

            var menuItemRepository = new MenuItemRepository(writeContext);
            await menuItemRepository.AddAsync(menuItem, CancellationToken.None);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new MenuItemRepository(readContext);

        var loaded = await readRepository.GetByIdAsync(menuItem.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Variants.Should().HaveCount(2);
        loaded.DefaultVariant!.Name.Should().Be("Small");
        loaded.BaseIngredients.Select(i => i.Id).Should().BeEquivalentTo(new[] { baseIngredient.Id });
        loaded.AllowedExtras.Select(i => i.Id).Should().BeEquivalentTo(new[] { extraIngredient.Id });
        loaded.BasePrice.Should().Be(menuItem.BasePrice);
    }

    [Fact]
    public async Task GetMenu_OnlyReturnsAvailableItems()
    {
        var baseIngredient = DomainTestFactory.CreateIngredient("Tomato sauce", 0m);
        var available = DomainTestFactory.CreatePizzaWithVariantsAndIngredients(baseIngredient, baseIngredient);
        var unavailable = DomainTestFactory.CreatePizzaWithVariantsAndIngredients(baseIngredient, baseIngredient);
        unavailable.MarkUnavailable();

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Ingredients.AddAsync(baseIngredient);
            await writeContext.MenuItems.AddRangeAsync(available, unavailable);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var repository = new MenuItemRepository(readContext);

        var menu = await repository.GetMenuAsync(CancellationToken.None);

        menu.Select(m => m.Id).Should().Contain(available.Id);
        menu.Select(m => m.Id).Should().NotContain(unavailable.Id);
    }
}
