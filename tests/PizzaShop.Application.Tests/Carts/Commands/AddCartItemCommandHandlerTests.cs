using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Carts;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Carts.Commands;

public class AddCartItemCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IMenuItemRepository> _menuItemRepository = new();
    private readonly Mock<IIngredientRepository> _ingredientRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public AddCartItemCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private AddCartItemCommandHandler CreateHandler() =>
        new(_cartRepository.Object, _menuItemRepository.Object, _ingredientRepository.Object, _unitOfWork.Object, _currentUser.Object, _clock.Object);

    private MenuItem SetUpExistingMenuItem(Guid menuItemId)
    {
        var menuItem = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(25m));
        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItemId, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);
        return menuItem;
    }

    [Fact]
    public async Task Handle_NoCurrentCustomer_ThrowsForbiddenOperationException()
    {
        _currentUser.Setup(c => c.CustomerId).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new AddCartItemCommand(Guid.NewGuid(), null, [], 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Handle_NoExistingCart_CreatesAndPersistsNewCart()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        SetUpExistingMenuItem(menuItemId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync((Cart?)null);

        Cart? added = null;
        _cartRepository
            .Setup(r => r.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((c, _) => added = c)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var itemId = await handler.Handle(new AddCartItemCommand(menuItemId, null, [], 2, null), CancellationToken.None);

        added.Should().NotBeNull();
        added!.CustomerId.Should().Be(customerId);
        added.Items.Should().ContainSingle(i => i.Id == itemId && i.MenuItemId == menuItemId && i.Quantity == 2);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCart_AddsItemWithoutCallingAddAsync()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        SetUpExistingMenuItem(menuItemId);
        var existingCart = Cart.Create(customerId, Now);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _cartRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(existingCart);

        var handler = CreateHandler();

        await handler.Handle(new AddCartItemCommand(menuItemId, null, [], 1, null), CancellationToken.None);

        existingCart.Items.Should().ContainSingle();
        _cartRepository.Verify(r => r.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MenuItemDoesNotExist_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _menuItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new AddCartItemCommand(Guid.NewGuid(), null, [], 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _cartRepository.Verify(r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_VariantDoesNotBelongToMenuItem_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        SetUpExistingMenuItem(menuItemId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var handler = CreateHandler();
        var unknownVariantId = Guid.NewGuid();

        var act = () => handler.Handle(new AddCartItemCommand(menuItemId, unknownVariantId, [], 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExtraNotAllowedForMenuItem_ThrowsExtraNotAllowedException()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        SetUpExistingMenuItem(menuItemId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var extraId = Guid.NewGuid();
        var extra = Ingredient.Create("Olives", new Money(3m));
        _ingredientRepository.Setup(r => r.GetByIdAsync(extraId, It.IsAny<CancellationToken>())).ReturnsAsync(extra);

        var handler = CreateHandler();

        // `extra` exists but was never added to the menu item's AllowedExtras.
        var act = () => handler.Handle(new AddCartItemCommand(menuItemId, null, [extraId], 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<PizzaShop.Domain.Exceptions.ExtraNotAllowedException>();
    }

    [Fact]
    public async Task Handle_ExtraDoesNotExist_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        SetUpExistingMenuItem(menuItemId);
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);
        _ingredientRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ingredient?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new AddCartItemCommand(menuItemId, null, [Guid.NewGuid()], 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
