using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Queries;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Catalog.Queries;

public class GetMenuQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedMenuItems()
    {
        var cheese = Ingredient.Create("Cheese", new Money(0));
        var pizza = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(20));
        pizza.AddBaseIngredient(cheese);

        var repository = new Mock<IMenuItemRepository>();
        repository.Setup(r => r.GetMenuAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MenuItem> { pizza });

        var handler = new GetMenuQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMenuQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(pizza.Id);
        result[0].Name.Should().Be("Margherita");
        result[0].BaseIngredients.Should().ContainSingle(i => i.Name == "Cheese");
    }
}
