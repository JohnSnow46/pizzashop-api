using Microsoft.EntityFrameworkCore;
using PizzaShop.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PizzaShop.Infrastructure.Tests.Fixtures;

/// <summary>
/// Spins up a real PostgreSQL container via Testcontainers for the lifetime of the test
/// collection (ADR-0025) — validates the actual Npgsql provider (converters, jsonb,
/// many-to-many, owned types, shadow properties), unlike EF InMemory/SQLite which would
/// silently ignore most of that mapping. Requires Docker; tests using this fixture are
/// tagged with the "Integration" trait so they can be excluded locally without Docker via
/// <c>dotnet test --filter Category!=Integration</c>.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pizzashop_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Fresh <see cref="PizzaShopDbContext"/> instance against the running container.</summary>
    public PizzaShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PizzaShopDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new PizzaShopDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
