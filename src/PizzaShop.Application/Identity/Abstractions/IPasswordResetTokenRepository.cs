namespace PizzaShop.Application.Identity.Abstractions;

/// <summary>
/// Repository for <see cref="PasswordResetToken"/>. Commit happens through
/// <see cref="Common.Abstractions.IUnitOfWork"/>, sharing the scoped <c>DbContext</c> with
/// <see cref="IUserAccountRepository"/>.
/// </summary>
public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>"Active" = not yet used and not yet expired as of <paramref name="now"/>.</summary>
    Task<IReadOnlyList<PasswordResetToken>> GetActiveByUserAccountIdAsync(Guid userAccountId, DateTimeOffset now, CancellationToken cancellationToken);

    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);
}
