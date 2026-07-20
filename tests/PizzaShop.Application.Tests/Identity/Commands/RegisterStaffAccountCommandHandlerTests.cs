using FluentAssertions;
using Moq;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Tests.Identity.Commands;

public class RegisterStaffAccountCommandHandlerTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    public RegisterStaffAccountCommandHandlerTests()
    {
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtTokenGenerator.Setup(g => g.Generate(It.IsAny<UserAccount>(), It.IsAny<Guid?>())).Returns("jwt-token");
        _clock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private RegisterStaffAccountCommandHandler CreateHandler() =>
        new(_userAccountRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object, _unitOfWork.Object, _currentUser.Object, _clock.Object);

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.RestaurantAdmin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Handle_SuperAdminCaller_CanCreateAnyStaffRole(UserRole targetRole)
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.SuperAdmin);

        UserAccount? added = null;
        _userAccountRepository
            .Setup(r => r.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .Callback<UserAccount, CancellationToken>((a, _) => added = a)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", targetRole), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Role.Should().Be(targetRole);
        result.Role.Should().Be(targetRole);
        result.CustomerId.Should().BeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RestaurantAdminCaller_CanCreateEmployee()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.RestaurantAdmin);

        var result = await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", UserRole.Employee), CancellationToken.None);

        result.Role.Should().Be(UserRole.Employee);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UserRole.RestaurantAdmin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Handle_RestaurantAdminCaller_CannotCreateAdminRoles_ThrowsForbiddenOperationException(UserRole targetRole)
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.RestaurantAdmin);

        var act = async () => await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", targetRole), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
        _userAccountRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmployeeCaller_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var act = async () => await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", UserRole.Employee), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_ExistingEmail_ThrowsConflictException()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.SuperAdmin);
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = async () => await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", UserRole.Employee), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ConcurrentRegistrationRaceDetectedAtCommit_PropagatesConflictException()
    {
        // Same race as RegisterCustomerCommandHandlerTests: two concurrent staff registrations
        // with the same email both pass ExistsByEmailAsync before either commits; the unique
        // index (mapped by UnitOfWork to ConflictException) is the actual backstop.
        _currentUser.Setup(c => c.Role).Returns(UserRole.SuperAdmin);
        _userAccountRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("The request conflicts with an existing record (a unique value is already in use)."));

        var act = async () => await CreateHandler().Handle(new RegisterStaffAccountCommand("staff@example.com", "Password123", UserRole.Employee), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
