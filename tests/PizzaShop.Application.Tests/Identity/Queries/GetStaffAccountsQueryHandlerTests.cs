using FluentAssertions;
using Moq;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Queries;

namespace PizzaShop.Application.Tests.Identity.Queries;

public class GetStaffAccountsQueryHandlerTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();

    private GetStaffAccountsQueryHandler CreateHandler() => new(_userAccountRepository.Object);

    [Fact]
    public async Task Handle_ReturnsStaffAccountsAsDtos()
    {
        var employee = UserAccount.Create("employee@pizzashop.test", "hash", UserRole.Employee, DateTimeOffset.UtcNow);
        var admin = UserAccount.Create("admin@pizzashop.test", "hash", UserRole.RestaurantAdmin, DateTimeOffset.UtcNow);
        _userAccountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccount> { employee, admin });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetStaffAccountsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(dto => dto.Id == employee.Id && dto.Role == UserRole.Employee);
        result.Should().Contain(dto => dto.Id == admin.Id && dto.Role == UserRole.RestaurantAdmin);
    }

    [Fact]
    public async Task Handle_ExcludesCustomerAccounts()
    {
        var customer = UserAccount.Create("customer@pizzashop.test", "hash", UserRole.Customer, DateTimeOffset.UtcNow);
        var employee = UserAccount.Create("employee@pizzashop.test", "hash", UserRole.Employee, DateTimeOffset.UtcNow);
        _userAccountRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccount> { customer, employee });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetStaffAccountsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(employee.Id);
    }
}
