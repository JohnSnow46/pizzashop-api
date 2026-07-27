using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Storage;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Commands;

public class UploadMenuItemImageCommandHandlerTests
{
    private readonly Mock<IMenuItemRepository> _menuItemRepository = new();
    private readonly Mock<IMenuItemImageStorage> _menuItemImageStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UploadMenuItemImageCommandHandler CreateHandler() =>
        new(_menuItemRepository.Object, _menuItemImageStorage.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SavesImageAndUpdatesMenuItem_ReturnsNewUrl()
    {
        var menuItem = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(20), "Classic", "/uploads/menu-items/old.jpg");
        var content = new byte[] { 1, 2, 3 };
        const string newUrl = "/uploads/menu-items/new-id.png";

        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);
        _menuItemImageStorage
            .Setup(s => s.SaveAsync(menuItem.Id, content, ".png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUrl);

        var command = new UploadMenuItemImageCommand(menuItem.Id, content, "image/png", ".png");
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(newUrl);
        menuItem.ImageUrl.Should().Be(newUrl);
        menuItem.Description.Should().Be("Classic");
        _menuItemRepository.Verify(r => r.UpdateAsync(menuItem, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MenuItemNotFound_ThrowsNotFoundException()
    {
        _menuItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);

        var command = new UploadMenuItemImageCommand(Guid.NewGuid(), new byte[] { 1 }, "image/png", ".png");
        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _menuItemImageStorage.Verify(
            s => s.SaveAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
