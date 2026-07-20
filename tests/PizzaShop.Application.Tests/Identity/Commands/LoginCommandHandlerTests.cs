using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Domain.Customers;

namespace PizzaShop.Application.Tests.Identity.Commands;

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();

    private LoginCommandHandler CreateHandler() =>
        new(_userAccountRepository.Object, _customerRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);

    private static UserAccount CreateAccount(UserRole role) =>
        UserAccount.Create("jan@example.com", "hashed-password", role, Now);

    [Fact]
    public async Task Handle_ValidCustomerCredentials_ReturnsTokenWithCustomerId()
    {
        var account = CreateAccount(UserRole.Customer);
        var customer = Customer.Create(account.Id, "Jan Kowalski", account.Email, Now);

        _userAccountRepository.Setup(r => r.GetByEmailAsync("jan@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _customerRepository.Setup(r => r.GetByUserAccountIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _passwordHasher.Setup(h => h.Verify("Password123", account.PasswordHash)).Returns(true);
        _jwtTokenGenerator.Setup(g => g.Generate(account, customer.Id)).Returns("jwt-token");

        var result = await CreateHandler().Handle(new LoginCommand("Jan@Example.com", "Password123"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        result.UserAccountId.Should().Be(account.Id);
        result.Role.Should().Be(UserRole.Customer);
        result.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public async Task Handle_ValidStaffCredentials_ReturnsTokenWithoutCustomerId()
    {
        var account = CreateAccount(UserRole.Employee);

        _userAccountRepository.Setup(r => r.GetByEmailAsync("jan@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordHasher.Setup(h => h.Verify("Password123", account.PasswordHash)).Returns(true);
        _jwtTokenGenerator.Setup(g => g.Generate(account, null)).Returns("jwt-token");

        var result = await CreateHandler().Handle(new LoginCommand("jan@example.com", "Password123"), CancellationToken.None);

        result.CustomerId.Should().BeNull();
        _customerRepository.Verify(r => r.GetByUserAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsInvalidCredentialsException()
    {
        _userAccountRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAccount?)null);

        var act = async () => await CreateHandler().Handle(new LoginCommand("unknown@example.com", "Password123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var account = CreateAccount(UserRole.Customer);
        _userAccountRepository.Setup(r => r.GetByEmailAsync("jan@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = async () => await CreateHandler().Handle(new LoginCommand("jan@example.com", "WrongPassword"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_ThrowsInvalidCredentialsException()
    {
        var account = CreateAccount(UserRole.Customer);
        account.Deactivate();
        _userAccountRepository.Setup(r => r.GetByEmailAsync("jan@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var act = async () => await CreateHandler().Handle(new LoginCommand("jan@example.com", "Password123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}
