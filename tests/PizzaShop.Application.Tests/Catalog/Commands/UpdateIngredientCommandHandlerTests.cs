using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Commands;

public class UpdateIngredientCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesNamePriceAndAvailability()
    {
        var ingredient = Ingredient.Create("Old", new Money(1));
        var repository = new Mock<IIngredientRepository>();
        repository.Setup(r => r.GetByIdAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateIngredientCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(new UpdateIngredientCommand(ingredient.Id, "New", new MoneyDto(4, "PLN"), false), CancellationToken.None);

        ingredient.Name.Should().Be("New");
        ingredient.ExtraPrice.Amount.Should().Be(4);
        ingredient.IsAvailable.Should().BeFalse();
        repository.Verify(r => r.UpdateAsync(ingredient, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IngredientNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IIngredientRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ingredient?)null);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateIngredientCommandHandler(repository.Object, unitOfWork.Object);

        var act = () => handler.Handle(new UpdateIngredientCommand(Guid.NewGuid(), "X", new MoneyDto(1, "PLN"), true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
