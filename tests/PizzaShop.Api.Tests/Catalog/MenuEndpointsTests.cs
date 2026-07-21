using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Catalog;

/// <summary>
/// End-to-end tests for <c>/api/menu</c> (api-layer.md 6.2): routing, JWT authorization
/// (anonymous vs. role), the CQRS dispatcher and FluentValidation, through the real HTTP
/// pipeline (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class MenuEndpointsTests : IClassFixture<ApiTestFactory>
{
    // Program.cs serializes controller responses with a JsonStringEnumConverter (enums as
    // strings) — deserializing DTOs that carry an enum (MenuItemDto.Category) needs the same
    // converter on the read side.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiTestFactory _factory;

    public MenuEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static CreateMenuItemCommand ValidDrinkCommand(string name = "Cola") =>
        new(name, MenuCategory.Drink, new MoneyDto(9.99m, "PLN"), "Cold drink", null,
            Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<MenuItemVariantInputDto>());

    [Fact]
    public async Task GetMenu_Anonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/menu");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/menu/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/menu", ValidDrinkCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/menu", ValidDrinkCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAdminRole_CreatesMenuItemAndIsRetrievableAndListed()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);

        var createResponse = await client.PostAsJsonAsync("/api/menu", ValidDrinkCommand("Sprite"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();

        var getResponse = await client.GetAsync($"/api/menu/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        dto!.Name.Should().Be("Sprite");

        var listResponse = await client.GetAsync("/api/menu");
        var list = await listResponse.Content.ReadFromJsonAsync<List<MenuItemDto>>(JsonOptions);
        list.Should().Contain(i => i.Id == id);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();
        var updateCommand = new UpdateMenuItemCommand(
            id,
            "Fanta Zero",
            "Sugar-free",
            null,
            new MoneyDto(8.5m, "PLN"),
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<MenuItemVariantInputDto>());

        var response = await client.PutAsJsonAsync($"/api/menu/{id}", updateCommand);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var id = Guid.NewGuid();
        var updateCommand = new UpdateMenuItemCommand(
            id,
            "Fanta Zero",
            "Sugar-free",
            null,
            new MoneyDto(8.5m, "PLN"),
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<MenuItemVariantInputDto>());

        var response = await client.PutAsJsonAsync($"/api/menu/{id}", updateCommand);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithAdminRole_UpdatesMenuItem()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var createResponse = await client.PostAsJsonAsync("/api/menu", ValidDrinkCommand("Fanta"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateCommand = new UpdateMenuItemCommand(
            id,
            "Fanta Zero",
            "Sugar-free",
            null,
            new MoneyDto(8.5m, "PLN"),
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<MenuItemVariantInputDto>());

        var updateResponse = await client.PutAsJsonAsync($"/api/menu/{id}", updateCommand);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/menu/{id}");
        var dto = await getResponse.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        dto!.Name.Should().Be("Fanta Zero");
    }

    [Fact]
    public async Task SetAvailability_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.PatchAsJsonAsync(
            $"/api/menu/{id}/availability",
            new SetMenuItemAvailabilityCommand(id, false));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetAvailability_WithStaffRole_UpdatesAvailability()
    {
        var adminClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var createResponse = await adminClient.PostAsJsonAsync("/api/menu", ValidDrinkCommand("Water"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var employeeClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await employeeClient.PatchAsJsonAsync(
            $"/api/menu/{id}/availability",
            new SetMenuItemAvailabilityCommand(id, false));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"/api/menu/{id}");
        var dto = await getResponse.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        dto!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task SetAvailability_WithCustomerRole_ReturnsForbidden()
    {
        var adminClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var createResponse = await adminClient.PostAsJsonAsync("/api/menu", ValidDrinkCommand("Juice"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await customerClient.PatchAsJsonAsync(
            $"/api/menu/{id}/availability",
            new SetMenuItemAvailabilityCommand(id, false));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
