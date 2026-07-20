using FluentAssertions;
using PizzaShop.Infrastructure.Tests.Fixtures;

namespace PizzaShop.Infrastructure.Tests.Persistence;

/// <summary>
/// Smoke tests that build the EF Core model and apply migrations against a real PostgreSQL
/// container (ADR-0025) — catches mapping mistakes (bad conventions, missing keys, ambiguous
/// many-to-many, etc.) before they surface at runtime.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ModelBuildingTests
{
    private readonly PostgresFixture _fixture;

    public ModelBuildingTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Model_BuildsWithoutErrors()
    {
        using var context = _fixture.CreateContext();

        var model = context.Model;

        model.Should().NotBeNull();
        model.GetEntityTypes().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Database_CanConnectAfterMigrate()
    {
        await using var context = _fixture.CreateContext();

        var canConnect = await context.Database.CanConnectAsync();

        canConnect.Should().BeTrue();
    }
}
