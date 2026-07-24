using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Queries;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Queries;

public class GetIngredientsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedIngredients()
    {
        var cheese = Ingredient.Create("Cheese", new Money(0));
        var olives = Ingredient.Create("Olives", new Money(2));

        var repository = new Mock<IIngredientRepository>();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { cheese, olives });

        var handler = new GetIngredientsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetIngredientsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(i => i.Name == "Cheese");
        result.Should().ContainSingle(i => i.Name == "Olives");
    }
}
