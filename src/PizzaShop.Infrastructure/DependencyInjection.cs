using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Infrastructure.Geocoding;
using PizzaShop.Infrastructure.Loyalty;
using PizzaShop.Infrastructure.Payments.PayU;
using PizzaShop.Infrastructure.Persistence;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Time;

namespace PizzaShop.Infrastructure;

/// <summary>
/// Composition root for this layer (ADR-0024, infrastructure-layer.md 8). Registers only the
/// ports Infrastructure owns — repositories, <see cref="IUnitOfWork"/>, <see cref="IPaymentGateway"/>,
/// <see cref="IGeocodingService"/>, <see cref="IClock"/>, <see cref="ILoyaltyPolicy"/>.
/// <see cref="Application.Common.Abstractions.ICurrentUser"/> and
/// <c>IOrderNotifier</c> are registered by Api instead.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PizzaShopDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();

        services.Configure<PayUOptions>(configuration.GetSection("PayU"));
        services.AddHttpClient<IPaymentGateway, PayUPaymentGateway>()
            // PayU signals a successful checkout session with a 302 redirect — the caller
            // (Application) needs that Location header, not the followed response.
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.Configure<GeocodingOptions>(configuration.GetSection("Geocoding"));
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ILoyaltyPolicy, LinearLoyaltyPolicy>();

        return services;
    }
}
