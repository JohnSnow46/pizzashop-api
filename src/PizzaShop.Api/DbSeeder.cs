using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Api;

/// <summary>
/// Bootstraps the initial <see cref="UserRole.SuperAdmin"/> account from configuration
/// (<c>Seed:SuperAdminEmail</c>/<c>Seed:SuperAdminPassword</c>) — without it, nobody could ever
/// create the first staff account through <c>POST /api/auth/staff</c> (api-layer.md 2.7,
/// ADR-0026). Idempotent: does nothing once an account with that email already exists, and
/// does nothing at all if the configuration keys are missing (e.g. a fresh dev box that hasn't
/// set up user-secrets yet).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider rootServices, CancellationToken cancellationToken = default)
    {
        await using var scope = rootServices.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["Seed:SuperAdminEmail"];
        var password = configuration["Seed:SuperAdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var userAccountRepository = services.GetRequiredService<IUserAccountRepository>();
        var normalizedEmail = UserAccount.NormalizeEmail(email);

        if (await userAccountRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            return;

        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var clock = services.GetRequiredService<IClock>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var superAdmin = UserAccount.Create(normalizedEmail, passwordHasher.Hash(password), UserRole.SuperAdmin, clock.UtcNow);

        await userAccountRepository.AddAsync(superAdmin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
