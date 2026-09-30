using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Tests.Carts.Commands;

public class ClearCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public ClearCartCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private ClearCartCommandHandler CreateHandler() =>
        new(_cartRepository.Object, _unitOfWork.Object, _currentUser.Object, _clock.Object);

    [Fact]
    public async Task Handle_NoCart_IsNoOpAndDoesNotSave()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync((Cart?)null);

        var handler = CreateHandler();

        await handler.Handle(new ClearCartCommand(), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CartWithItems_ClearsThem()
    {
        var customerId = Guid.NewGuid();
        var cart = Cart.Create(customerId, Now);
        cart.AddItem(Guid.NewGuid(), null, [], 1, null, Now);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

        var handler = CreateHandler();

        await handler.Handle(new ClearCartCommand(), CancellationToken.None);

        cart.Items.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
