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
///
/// The container/database is created ONCE for the whole <c>PostgresCollection</c> (migrating
/// on every test would be far too slow) — per-test isolation is instead provided by
/// <see cref="ResetAsync"/>, which every test class in the collection must invoke before each
/// test method runs (see <see cref="PostgresRepositoryTestBase"/>).
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

    /// <summary>
    /// Truncates every table mapped by the EF Core model (business tables plus owned-type/
    /// many-to-many join tables) so each test method starts from an empty database, regardless
    /// of what a previous test in the shared collection inserted — the container/schema stays,
    /// only the data is wiped. Table names are read from the live model rather than hardcoded so
    /// this keeps working as entities are added/renamed.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        var tableNames = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(name => name is not null)
            .Distinct()
            .ToList();

        if (tableNames.Count == 0)
        {
            return;
        }

        var quotedTables = string.Join(", ", tableNames.Select(name => $"\"{name}\""));

        // Table names come exclusively from EF Core's own metadata, never from external/user
        // input, so building the statement this way (rather than a parameterized query, which
        // Postgres doesn't support for identifiers) is safe — assigning to a plain string first
        // avoids the interpolated-string overload EF1002 warns about.
        string truncateStatement = "TRUNCATE TABLE " + quotedTables + " RESTART IDENTITY CASCADE;";
        await context.Database.ExecuteSqlRawAsync(truncateStatement);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
