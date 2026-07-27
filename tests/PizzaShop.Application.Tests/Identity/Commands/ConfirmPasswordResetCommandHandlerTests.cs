using FluentAssertions;
using Moq;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Tests.Identity.Commands;

public class ConfirmPasswordResetCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public ConfirmPasswordResetCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("new-hash");
    }

    private ConfirmPasswordResetCommandHandler CreateHandler() =>
        new(
            _passwordResetTokenRepository.Object,
            _userAccountRepository.Object,
            _passwordHasher.Object,
            _unitOfWork.Object,
            _clock.Object);

    private static UserAccount ExistingAccount() =>
        UserAccount.Create("jan@example.com", "old-hash", UserRole.Customer, Now);

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordHashAndConsumesToken()
    {
        var account = ExistingAccount();
        var resetToken = PasswordResetToken.Create(account.Id, Now);

        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(resetToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);
        _userAccountRepository.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        await CreateHandler().Handle(new ConfirmPasswordResetCommand(resetToken.Token, "NewPassword123"), CancellationToken.None);

        account.PasswordHash.Should().Be("new-hash");
        resetToken.UsedAt.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsConflictException()
    {
        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PasswordResetToken?)null);

        var act = async () => await CreateHandler().Handle(new ConfirmPasswordResetCommand("missing-token", "NewPassword123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsConflictException()
    {
        var account = ExistingAccount();
        var resetToken = PasswordResetToken.Create(account.Id, Now);
        _clock.Setup(c => c.UtcNow).Returns(Now + PasswordResetToken.Lifetime + TimeSpan.FromMinutes(1));

        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(resetToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);

        var act = async () => await CreateHandler().Handle(new ConfirmPasswordResetCommand(resetToken.Token, "NewPassword123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsConflictException()
    {
        var account = ExistingAccount();
        var resetToken = PasswordResetToken.Create(account.Id, Now);
        resetToken.Consume(Now);

        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(resetToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);

        var act = async () => await CreateHandler().Handle(new ConfirmPasswordResetCommand(resetToken.Token, "NewPassword123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_ThrowsConflictExceptionAndDoesNotChangePassword()
    {
        var account = ExistingAccount();
        account.Deactivate();
        var resetToken = PasswordResetToken.Create(account.Id, Now);

        _passwordResetTokenRepository.Setup(r => r.GetByTokenAsync(resetToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);
        _userAccountRepository.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var act = async () => await CreateHandler().Handle(new ConfirmPasswordResetCommand(resetToken.Token, "NewPassword123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        account.PasswordHash.Should().Be("old-hash");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
