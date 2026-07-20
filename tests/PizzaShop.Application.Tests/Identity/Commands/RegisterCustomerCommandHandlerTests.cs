using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Tests.Identity.Commands;

public class RegisterCustomerCommandHandlerTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public RegisterCustomerCommandHandlerTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtTokenGenerator.Setup(g => g.Generate(It.IsAny<UserAccount>(), It.IsAny<Guid?>())).Returns("jwt-token");
        _clock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private RegisterCustomerCommandHandler CreateHandler() =>
        new(
            _userAccountRepository.Object,
            _customerRepository.Object,
            _loyaltyAccountRepository.Object,
            _passwordHasher.Object,
            _jwtTokenGenerator.Object,
            _unitOfWork.Object,
            _clock.Object);

    private static RegisterCustomerCommand ValidCommand() =>
        new("Jan@Example.com", "Password123", "Jan Kowalski", "123456789");

    [Fact]
    public async Task Handle_NewEmail_CreatesLinkedUserAccountCustomerAndLoyaltyAccount()
    {
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        UserAccount? addedAccount = null;
        _userAccountRepository
            .Setup(r => r.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .Callback<UserAccount, CancellationToken>((a, _) => addedAccount = a)
            .Returns(Task.CompletedTask);

        LoyaltyAccount? addedLoyaltyAccount = null;
        _loyaltyAccountRepository
            .Setup(r => r.AddAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()))
            .Callback<LoyaltyAccount, CancellationToken>((a, _) => addedLoyaltyAccount = a)
            .Returns(Task.CompletedTask);

        Customer? addedCustomer = null;
        _customerRepository
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => addedCustomer = c)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        addedAccount.Should().NotBeNull();
        addedAccount!.Email.Should().Be("jan@example.com");
        addedAccount.Role.Should().Be(UserRole.Customer);

        addedCustomer.Should().NotBeNull();
        addedCustomer!.UserAccountId.Should().Be(addedAccount.Id);
        addedLoyaltyAccount!.CustomerId.Should().Be(addedCustomer.Id);

        result.Token.Should().Be("jwt-token");
        result.UserAccountId.Should().Be(addedAccount.Id);
        result.CustomerId.Should().Be(addedCustomer.Id);
        result.Role.Should().Be(UserRole.Customer);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ThrowsConflictException()
    {
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _userAccountRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrentRegistrationRaceDetectedAtCommit_PropagatesConflictException()
    {
        // Simulates two concurrent registrations with the same email both passing
        // ExistsByEmailAsync before either commits (api-layer.md 2.6). The unique index is the
        // actual backstop; UnitOfWork maps that low-level violation onto ConflictException
        // (infrastructure-layer.md/UnitOfWork). This test asserts the handler does not swallow
        // or re-wrap it — it propagates as-is, still a 409.
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("The request conflicts with an existing record (a unique value is already in use)."));

        var act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
