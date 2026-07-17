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

public class CreateMenuItemCommandHandlerTests
{
    private readonly Mock<IMenuItemRepository> _menuItemRepository = new();
    private readonly Mock<IIngredientRepository> _ingredientRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateMenuItemCommandHandler CreateHandler() =>
        new(_menuItemRepository.Object, _ingredientRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidPizza_CreatesMenuItemWithIngredientsAndVariant()
    {
        var cheese = Ingredient.Create("Cheese", new Money(0));
        var olives = Ingredient.Create("Olives", new Money(3));

        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(cheese.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { cheese });
        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(olives.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { olives });

        MenuItem? added = null;
        _menuItemRepository
            .Setup(r => r.AddAsync(It.IsAny<MenuItem>(), It.IsAny<CancellationToken>()))
            .Callback<MenuItem, CancellationToken>((item, _) => added = item)
            .Returns(Task.CompletedTask);

        var command = new CreateMenuItemCommand(
            "Margherita",
            MenuCategory.Pizza,
            new MoneyDto(30, "PLN"),
            "Classic",
            null,
            new[] { cheese.Id },
            new[] { olives.Id },
            new[] { new MenuItemVariantInputDto(null, "Large", new MoneyDto(35, "PLN"), true) });

        var handler = CreateHandler();

        var id = await handler.Handle(command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.Id.Should().Be(id);
        added.BaseIngredients.Should().ContainSingle(i => i.Id == cheese.Id);
        added.AllowedExtras.Should().ContainSingle(i => i.Id == olives.Id);
        added.Variants.Should().ContainSingle(v => v.Name == "Large");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PizzaWithoutBaseIngredients_ThrowsPizzaWithoutIngredientException()
    {
        var command = new CreateMenuItemCommand(
            "Margherita",
            MenuCategory.Pizza,
            new MoneyDto(30, "PLN"),
            null,
            null,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<MenuItemVariantInputDto>());

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PizzaWithoutIngredientException>();
        _menuItemRepository.Verify(r => r.AddAsync(It.IsAny<MenuItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownBaseIngredientId_ThrowsNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient>());

        var command = new CreateMenuItemCommand(
            "Margherita",
            MenuCategory.Pizza,
            new MoneyDto(30, "PLN"),
            null,
            null,
            new[] { unknownId },
            Array.Empty<Guid>(),
            Array.Empty<MenuItemVariantInputDto>());

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _menuItemRepository.Verify(r => r.AddAsync(It.IsAny<MenuItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
