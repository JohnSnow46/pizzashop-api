using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PizzaShop.Api;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for Iteration 1 Api tests
/// (api-layer.md 11 step 12). Boots the real Program.cs pipeline (routing, JWT
/// authentication/authorization, controllers, the CQRS dispatcher, FluentValidation, and
/// <c>PizzaShop.Api.Middleware.ExceptionHandler</c>), but swaps the Identity module's
/// persistence (<see cref="IUserAccountRepository"/>, <see cref="ICustomerRepository"/>,
/// <see cref="ILoyaltyAccountRepository"/>, <see cref="IUnitOfWork"/>) for in-memory fakes.
/// This project has no Testcontainers/Docker dependency (that's
/// PizzaShop.Infrastructure.Tests, ADR-0025); it verifies the Api-layer wiring, not EF Core
/// persistence. <c>UseEnvironment("Testing")</c> also makes Program.cs skip
/// <c>Database.MigrateAsync()</c>/<c>PizzaShop.Api.DbSeeder</c>, which would otherwise need a
/// real Postgres connection.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string SuperAdminEmail = "superadmin@pizzashop.test";
    public const string SuperAdminPassword = "SuperSecret123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "api-tests-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "PizzaShop.Api.Tests",
                ["Jwt:Audience"] = "PizzaShop.Api.Tests",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Seed:SuperAdminEmail"] = SuperAdminEmail,
                ["Seed:SuperAdminPassword"] = SuperAdminPassword,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserAccountRepository>();
            services.AddSingleton<IUserAccountRepository, InMemoryUserAccountRepository>();

            services.RemoveAll<ICustomerRepository>();
            services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();

            services.RemoveAll<ILoyaltyAccountRepository>();
            services.AddSingleton<ILoyaltyAccountRepository, InMemoryLoyaltyAccountRepository>();

            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork, NoopUnitOfWork>();

            services.RemoveAll<IMenuItemRepository>();
            services.AddSingleton<IMenuItemRepository, InMemoryMenuItemRepository>();

            services.RemoveAll<IIngredientRepository>();
            services.AddSingleton<IIngredientRepository, InMemoryIngredientRepository>();

            services.RemoveAll<IRestaurantRepository>();
            services.AddSingleton<IRestaurantRepository, InMemoryRestaurantRepository>();

            services.RemoveAll<IPromotionRepository>();
            services.AddSingleton<IPromotionRepository, InMemoryPromotionRepository>();
        });
    }

    /// <summary>
    /// Program.cs deliberately skips <c>Database.MigrateAsync()</c>/<c>DbSeeder</c> in the
    /// "Testing" environment (no real Postgres here). Seed the SuperAdmin bootstrap account
    /// explicitly instead, directly against the in-memory fakes configured above, once the
    /// host (and its DI container) is fully built.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        DbSeeder.SeedAsync(host.Services).GetAwaiter().GetResult();
        return host;
    }
}
