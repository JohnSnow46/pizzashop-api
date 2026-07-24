using System.Collections.Concurrent;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IUserAccountRepository"/> used by <see cref="ApiTestFactory"/> so
/// Api integration tests exercise the full HTTP/auth/exception-mapping pipeline without a real
/// Postgres instance (round-trip persistence itself is already covered by
/// PizzaShop.Infrastructure.Tests via Testcontainers, ADR-0025). Registered as a singleton so
/// state survives across requests within one test (e.g. register then login).
/// </summary>
public sealed class InMemoryUserAccountRepository : IUserAccountRepository
{
    /// <summary>
    /// Sentinel email that makes <see cref="GetByEmailAsync"/> throw an exception no
    /// <c>PizzaShop.Api.Middleware.ExceptionHandler</c> branch recognizes, letting tests
    /// exercise its 500/<c>ProblemDetails</c> fallback without adding a test-only endpoint.
    /// </summary>
    public const string PoisonEmail = "boom@pizzashop.test";

    private readonly ConcurrentDictionary<Guid, UserAccount> _accounts = new();

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (email == PoisonEmail)
            throw new TimeoutException("Simulated unexpected failure for the Api exception-handler test.");

        return Task.FromResult(_accounts.Values.FirstOrDefault(a => a.Email == email));
    }

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.TryGetValue(id, out var account) ? account : null);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.Values.Any(a => a.Email == email));

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(_accounts.Values.OrderBy(a => a.CreatedAt).ToList());
}
