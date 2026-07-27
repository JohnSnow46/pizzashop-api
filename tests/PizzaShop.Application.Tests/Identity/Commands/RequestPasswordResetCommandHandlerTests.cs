using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Email;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Tests.Identity.Commands;

public class RequestPasswordResetCommandHandlerTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RequestPasswordResetCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _passwordResetTokenRepository
            .Setup(r => r.GetActiveByUserAccountIdAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PasswordResetToken>());
    }

    private RequestPasswordResetCommandHandler CreateHandler() =>
        new(
            _userAccountRepository.Object,
            _passwordResetTokenRepository.Object,
            _emailSender.Object,
            _unitOfWork.Object,
            _clock.Object);

    private static UserAccount ActiveAccount() =>
        UserAccount.Create("jan@example.com", "hashed-password", UserRole.Customer, Now);

    [Fact]
    public async Task Handle_ExistingActiveAccount_CreatesTokenAndSendsEmail()
    {
        var account = ActiveAccount();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);

        await CreateHandler().Handle(new RequestPasswordResetCommand("Jan@Example.com"), CancellationToken.None);

        _passwordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendPasswordResetEmailAsync(account.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownEmail_DoesNotCreateTokenOrSendEmail_ReturnsUnit()
    {
        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserAccount?)null);

        var act = async () => await CreateHandler().Handle(new RequestPasswordResetCommand("unknown@example.com"), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _passwordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_DoesNotCreateTokenOrSendEmail()
    {
        var account = ActiveAccount();
        account.Deactivate();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);

        await CreateHandler().Handle(new RequestPasswordResetCommand(account.Email), CancellationToken.None);

        _passwordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingActiveTokens_InvalidatesThemBeforeIssuingNew()
    {
        var account = ActiveAccount();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var existingToken = PasswordResetToken.Create(account.Id, Now.AddMinutes(-1));
        _passwordResetTokenRepository
            .Setup(r => r.GetActiveByUserAccountIdAsync(account.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingToken });

        await CreateHandler().Handle(new RequestPasswordResetCommand(account.Email), CancellationToken.None);

        existingToken.UsedAt.Should().NotBeNull();
    }
}
