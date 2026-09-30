using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Tests.Carts.Commands;

public class UpdateCartItemQuantityCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public UpdateCartItemQuantityCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private UpdateCartItemQuantityCommandHandler CreateHandler() =>
        new(_cartRepository.Object, _unitOfWork.Object, _currentUser.Object, _clock.Object);

    [Fact]
    public async Task Handle_NoCurrentCustomer_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new UpdateCartItemQuantityCommand(Guid.NewGuid(), 2), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_NoCart_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync((Cart?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new UpdateCartItemQuantityCommand(Guid.NewGuid(), 2), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var cart = Cart.Create(customerId, Now);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

        var handler = CreateHandler();

        var act = () => handler.Handle(new UpdateCartItemQuantityCommand(Guid.NewGuid(), 2), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExistingItem_UpdatesQuantity()
    {
        var customerId = Guid.NewGuid();
        var cart = Cart.Create(customerId, Now);
        var itemId = cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

        var handler = CreateHandler();

        await handler.Handle(new UpdateCartItemQuantityCommand(itemId, 9), CancellationToken.None);

        cart.Items.Single().Quantity.Should().Be(9);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
