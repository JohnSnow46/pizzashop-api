using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Identity;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;
using PizzaShop.Infrastructure.Persistence.Converters;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Infrastructure.Persistence;

/// <summary>
/// Single EF Core <see cref="DbContext"/> for the whole domain (ADR-0020). Exposes only
/// aggregate roots as <see cref="DbSet{TEntity}"/>; child entities/owned types are reached
/// through their root's navigation (infrastructure-layer.md 4.1).
/// </summary>
public sealed class PizzaShopDbContext : DbContext
{
    /// <summary>Name of the PostgreSQL sequence backing <see cref="Repositories.OrderRepository.NextOrderNumberAsync"/>.</summary>
    public const string OrderNumberSequenceName = "order_number_seq";

    public PizzaShopDbContext(DbContextOptions<PizzaShopDbContext> options)
        : base(options)
    {
    }

    public DbSet<DomainRestaurant> Restaurants => Set<DomainRestaurant>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    /// <summary>
    /// The only non-Domain entity in this context — identity deliberately lives outside
    /// Domain (ADR-0005/0026).
    /// </summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(OrderNumberSequenceName).StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PizzaShopDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Global Money <-> numeric(12,2) mapping (ADR-0020, infrastructure-layer.md 2.2) —
        // applies to every Money and Money? property without repeating the conversion.
        configurationBuilder.Properties<Money>()
            .HaveConversion<MoneyConverter>()
            .HavePrecision(12, 2);

        base.ConfigureConventions(configurationBuilder);
    }
}
