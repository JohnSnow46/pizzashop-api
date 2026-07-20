namespace PizzaShop.Application.Identity.Abstractions;

/// <summary>
/// Repository for <see cref="UserAccount"/> (api-layer.md 2.3, ADR-0026). Callers pass an
/// already-normalized email (<see cref="UserAccount.NormalizeEmail"/>). Commit happens through
/// <see cref="Common.Abstractions.IUnitOfWork"/>, sharing the scoped <c>DbContext</c> with
/// <c>ICustomerRepository</c>/<c>ILoyaltyAccountRepository</c> so customer registration is
/// atomic (api-layer.md 2.6).
/// </summary>
public interface IUserAccountRepository
{
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
}
