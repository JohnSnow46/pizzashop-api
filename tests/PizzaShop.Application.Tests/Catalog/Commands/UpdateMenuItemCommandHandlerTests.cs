using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Commands;

public class UpdateMenuItemCommandHandlerTests
{
    private readonly Mock<IMenuItemRepository> _menuItemRepository = new();
    private readonly Mock<IIngredientRepository> _ingredientRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateMenuItemCommandHandler CreateHandler() =>
        new(_menuItemRepository.Object, _ingredientRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ReconcilesIngredientsAndVariants()
    {
        var oldExtra = Ingredient.Create("Old", new Money(1));
        var newExtra = Ingredient.Create("New", new Money(2));
        var baseIngredient = Ingredient.Create("Base", new Money(0));

        var menuItem = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(20));
        menuItem.AddBaseIngredient(baseIngredient);
        menuItem.AllowExtra(oldExtra);
        var existingVariant = MenuItemVariant.Create("Small", new Money(20), isDefault: true);
        menuItem.AddVariant(existingVariant);

        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);
        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(newExtra.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { newExtra });

        var command = new UpdateMenuItemCommand(
            menuItem.Id,
            "Margherita Deluxe",
            "New description",
            "https://example.com/new.jpg",
            new MoneyDto(25, "PLN"),
            new[] { baseIngredient.Id },
            new[] { newExtra.Id },
            new[]
            {
                new MenuItemVariantInputDto(existingVariant.Id, "Small", new MoneyDto(22, "PLN"), true),
                new MenuItemVariantInputDto(null, "Large", new MoneyDto(30, "PLN"), false),
            });

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        menuItem.Name.Should().Be("Margherita Deluxe");
        menuItem.Description.Should().Be("New description");
        menuItem.ImageUrl.Should().Be("https://example.com/new.jpg");
        menuItem.BasePrice.Amount.Should().Be(25);
        menuItem.AllowedExtras.Should().ContainSingle(i => i.Id == newExtra.Id);
        menuItem.AllowedExtras.Should().NotContain(i => i.Id == oldExtra.Id);
        menuItem.Variants.Should().Contain(v => v.Id == existingVariant.Id && v.Price.Amount == 22);
        menuItem.Variants.Should().Contain(v => v.Name == "Large");
        _menuItemRepository.Verify(r => r.UpdateAsync(menuItem, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MenuItemNotFound_ThrowsNotFoundException()
    {
        _menuItemRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuItem?)null);

        var command = new UpdateMenuItemCommand(
            Guid.NewGuid(), "X", null, null, new MoneyDto(1, "PLN"), Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<MenuItemVariantInputDto>());

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownVariantId_ThrowsNotFoundException()
    {
        var menuItem = MenuItem.Create("Drink", MenuCategory.Drink, new Money(5));
        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);

        var command = new UpdateMenuItemCommand(
            menuItem.Id,
            "Drink",
            null,
            null,
            new MoneyDto(5, "PLN"),
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { new MenuItemVariantInputDto(Guid.NewGuid(), "0.5L", new MoneyDto(5, "PLN"), true) });

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ChangesDefaultAndRemovesOldDefaultVariant_Succeeds()
    {
        var menuItem = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(20));
        menuItem.AddBaseIngredient(Ingredient.Create("Base", new Money(0)));
        var small = MenuItemVariant.Create("Small", new Money(20), isDefault: true);
        var large = MenuItemVariant.Create("Large", new Money(30));
        menuItem.AddVariant(small);
        menuItem.AddVariant(large);

        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);

        var command = new UpdateMenuItemCommand(
            menuItem.Id,
            "Margherita",
            null,
            null,
            new MoneyDto(20, "PLN"),
            menuItem.BaseIngredients.Select(i => i.Id).ToList(),
            Array.Empty<Guid>(),
            new[]
            {
                // "Small" (old default) is absent -> removed; "Large" becomes the new default.
                new MenuItemVariantInputDto(large.Id, "Large", new MoneyDto(30, "PLN"), true),
            });

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        menuItem.Variants.Should().ContainSingle().Which.Should().Be(large);
        menuItem.DefaultVariant.Should().Be(large);
    }

    [Fact]
    public async Task Handle_RemovingOldDefaultWithoutNominatingNewDefault_ThrowsInvalidVariantConfigurationException()
    {
        var menuItem = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(20));
        menuItem.AddBaseIngredient(Ingredient.Create("Base", new Money(0)));
        var small = MenuItemVariant.Create("Small", new Money(20), isDefault: true);
        var large = MenuItemVariant.Create("Large", new Money(30));
        menuItem.AddVariant(small);
        menuItem.AddVariant(large);

        _menuItemRepository.Setup(r => r.GetByIdAsync(menuItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(menuItem);

        var command = new UpdateMenuItemCommand(
            menuItem.Id,
            "Margherita",
            null,
            null,
            new MoneyDto(20, "PLN"),
            menuItem.BaseIngredients.Select(i => i.Id).ToList(),
            Array.Empty<Guid>(),
            new[]
            {
                // "Small" (default) absent -> tries to remove it, but "Large" isn't nominated as default.
                new MenuItemVariantInputDto(large.Id, "Large", new MoneyDto(30, "PLN"), false),
            });

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidVariantConfigurationException>();
    }
}
