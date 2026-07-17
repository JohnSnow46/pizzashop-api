using PizzaShop.Domain.Customers;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Customer"/> purchasing profile (application-layer.md 3.1,
/// ADR-0005). Identity (login/role) lives outside Domain as <c>UserAccount</c> — this
/// repository deals only with the domain profile.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Customer?> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken);

    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken);
}
