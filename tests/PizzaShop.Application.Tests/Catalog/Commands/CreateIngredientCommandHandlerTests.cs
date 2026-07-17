using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Tests.Catalog.Commands;

public class CreateIngredientCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAndPersistsIngredient()
    {
        Ingredient? added = null;
        var repository = new Mock<IIngredientRepository>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()))
            .Callback<Ingredient, CancellationToken>((ingredient, _) => added = ingredient)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateIngredientCommandHandler(repository.Object, unitOfWork.Object);

        var id = await handler.Handle(new CreateIngredientCommand("Mozzarella", new MoneyDto(3, "PLN"), "Cheese"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Id.Should().Be(id);
        added.Name.Should().Be("Mozzarella");
        added.ExtraPrice.Amount.Should().Be(3);
        added.Category.Should().Be("Cheese");
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
