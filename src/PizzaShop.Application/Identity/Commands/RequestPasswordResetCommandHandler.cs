using PizzaShop.Application.Abstractions.Email;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Handles <see cref="RequestPasswordResetCommand"/>. Deliberately silent for unknown or
/// deactivated accounts — mirrors the "no email enumeration" rule from
/// <see cref="Common.Exceptions.InvalidCredentialsException"/> — so a caller can never use this
/// endpoint to discover whether a given email is registered. Any pre-existing active tokens for
/// the account are invalidated before a new one is issued, so at most one token is redeemable
/// at a time.
/// </summary>
public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RequestPasswordResetCommandHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userAccountRepository = userAccountRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var email = UserAccount.NormalizeEmail(command.Email);
        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        // Deliberately silent for unknown/inactive accounts — never let this endpoint be used
        // to enumerate registered emails.
        if (account is not null && account.IsActive)
        {
            var now = _clock.UtcNow;

            var activeTokens = await _passwordResetTokenRepository.GetActiveByUserAccountIdAsync(account.Id, now, cancellationToken);
            foreach (var activeToken in activeTokens)
                activeToken.Invalidate(now);

            var resetToken = PasswordResetToken.Create(account.Id, now);
            await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailSender.SendPasswordResetEmailAsync(account.Email, resetToken.Token, cancellationToken);
        }

        return Unit.Value;
    }
}
