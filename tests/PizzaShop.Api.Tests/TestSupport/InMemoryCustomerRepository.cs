using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Customers;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="ICustomerRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.TryGetValue(id, out var customer) ? customer : null);

    public Task<Customer?> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.Values.FirstOrDefault(c => c.UserAccountId == userAccountId));

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }
}
