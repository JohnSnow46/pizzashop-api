using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>Handles <see cref="ConfirmPasswordResetCommand"/> — redeems a token and sets a new password hash.</summary>
public sealed class ConfirmPasswordResetCommandHandler : ICommandHandler<ConfirmPasswordResetCommand>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmPasswordResetCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(command.Token, cancellationToken)
            ?? throw new ConflictException("This password reset link is invalid or has expired.");

        var now = _clock.UtcNow;
        resetToken.Consume(now); // guard clause: used/expired -> ConflictException

        var account = await _userAccountRepository.GetByIdAsync(resetToken.UserAccountId, cancellationToken);

        // Same message as the not-found/expired/used cases above — a deactivated account
        // (e.g. between token issuance and redemption) must not be distinguishable from an
        // invalid token.
        if (account is null || !account.IsActive)
            throw new ConflictException("This password reset link is invalid or has expired.");

        account.SetPasswordHash(_passwordHasher.Hash(command.NewPassword));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
