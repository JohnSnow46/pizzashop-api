using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Carts.Queries;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Tests.Carts.Queries;

public class GetMyCartQueryHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private GetMyCartQueryHandler CreateHandler() => new(_cartRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NoCurrentCustomer_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new GetMyCartQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_NoCart_ReturnsEmptyCartDto()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync((Cart?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyCartQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CartWithItems_ReturnsMappedItems()
    {
        var customerId = Guid.NewGuid();
        var cart = Cart.Create(customerId, Now);
        var menuItemId = Guid.NewGuid();
        cart.AddItem(menuItemId, null, [], 3, "Extra spicy", Now);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyCartQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.MenuItemId == menuItemId && i.Quantity == 3 && i.Notes == "Extra spicy");
        result.UpdatedAt.Should().Be(Now);
    }
}
