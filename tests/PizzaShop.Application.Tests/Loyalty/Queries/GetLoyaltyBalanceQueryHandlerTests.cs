using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Loyalty.Queries;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Tests.Loyalty.Queries;

public class GetLoyaltyBalanceQueryHandlerTests
{
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetLoyaltyBalanceQueryHandler CreateHandler() =>
        new(_loyaltyAccountRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_LoggedInCustomer_ReturnsOwnBalanceAndHistory()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var account = LoyaltyAccount.Create(customerId);
        account.Earn(50, "Signup bonus", DateTimeOffset.UtcNow);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetLoyaltyBalanceQuery(), CancellationToken.None);

        result.PointsBalance.Should().Be(50);
        result.Transactions.Should().ContainSingle();
        result.Transactions[0].Reason.Should().Be("Signup bonus");
    }

    [Fact]
    public async Task Handle_MultipleTransactions_ReturnsThemSortedByOccurredAtDescending()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var now = DateTimeOffset.UtcNow;
        var account = LoyaltyAccount.Create(customerId);
        account.Earn(10, "Oldest", now.AddDays(-2));
        account.Earn(20, "Newest", now);
        account.Earn(30, "Middle", now.AddDays(-1));
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetLoyaltyBalanceQuery(), CancellationToken.None);

        result.Transactions.Select(t => t.Reason).Should().Equal("Newest", "Middle", "Oldest");
    }

    [Fact]
    public async Task Handle_NoCustomerId_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetLoyaltyBalanceQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyAccount?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new GetLoyaltyBalanceQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
