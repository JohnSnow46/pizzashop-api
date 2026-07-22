using Xunit;

namespace PizzaShop.Infrastructure.Tests.Fixtures;

/// <summary>
/// Common base for every test class in <see cref="PostgresCollection"/>. xUnit creates a new
/// instance of the test class per test method, so <see cref="IAsyncLifetime.InitializeAsync"/>
/// resetting the shared database here runs before EVERY test method — without it, tests in this
/// collection observe rows left behind by other test classes/methods sharing the same
/// container/database (e.g. two tests each inserting "the" single-tenant <c>Restaurant</c> row,
/// or two tests reusing the same hardcoded order number).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public abstract class PostgresRepositoryTestBase : IAsyncLifetime
{
    protected PostgresRepositoryTestBase(PostgresFixture fixture)
    {
        Fixture = fixture;
    }

    protected PostgresFixture Fixture { get; }

    public Task InitializeAsync() => Fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
