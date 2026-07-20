using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PizzaShop.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build a <see cref="PizzaShopDbContext"/> at design time (migrations)
/// without running the full Api host (ADR-0025, infrastructure-layer.md 4.4). The connection
/// string is read from the <c>PIZZASHOP_DB</c> environment variable, falling back to a local
/// default so <c>dotnet ef migrations add</c> works out of the box in dev.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PizzaShopDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=pizzashop;Username=postgres;Password=postgres";

    public PizzaShopDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PIZZASHOP_DB") ?? DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<PizzaShopDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PizzaShopDbContext(optionsBuilder.Options);
    }
}
