using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Api.Tests.Catalog;

/// <summary>
/// End-to-end tests for <c>/api/ingredients</c> (api-layer.md 6.3): routing, JWT authorization
/// (anonymous vs. role), the CQRS dispatcher, through the real HTTP pipeline
/// (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class IngredientsEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public IngredientsEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static CreateIngredientCommand ValidCommand(string name = "Mozzarella") =>
        new(name, new MoneyDto(3.5m, "PLN"), "Cheese");

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await client.GetAsync("/api/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithAdminRole_ReturnsCreatedIngredients()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        await client.PostAsJsonAsync("/api/ingredients", ValidCommand($"Oregano-{Guid.NewGuid()}"));

        var response = await client.GetAsync("/api/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ingredients = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
        ingredients.Should().NotBeNull();
        ingredients!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ingredients", ValidCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await client.PostAsJsonAsync("/api/ingredients", ValidCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAdminRole_CreatesIngredient()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);

        var response = await client.PostAsJsonAsync("/api/ingredients", ValidCommand("Basil"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_WithAdminRole_UpdatesIngredient()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var createResponse = await client.PostAsJsonAsync("/api/ingredients", ValidCommand("Olives"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateCommand = new UpdateIngredientCommand(id, "Black Olives", new MoneyDto(4m, "PLN"), false);

        var response = await client.PutAsJsonAsync($"/api/ingredients/{id}", updateCommand);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{id}",
            new UpdateIngredientCommand(id, "Ghost", new MoneyDto(1m, "PLN"), true));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{id}",
            new UpdateIngredientCommand(id, "Ghost", new MoneyDto(1m, "PLN"), true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{id}",
            new UpdateIngredientCommand(id, "Ghost", new MoneyDto(1m, "PLN"), true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
