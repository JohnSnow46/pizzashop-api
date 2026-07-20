using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Customers;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICustomerRepository"/> — the <see cref="Customer"/>
/// purchasing profile, address book included automatically as an owned collection
/// (ADR-0020).
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly PizzaShopDbContext _context;

    public CustomerRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(c => c.UserAccountId == userAccountId, cancellationToken);

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }
}
