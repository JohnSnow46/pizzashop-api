using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Api.Tests.Restaurant;

/// <summary>
/// End-to-end tests for <c>/api/restaurant</c> (api-layer.md 6.4): routing, JWT authorization
/// (anonymous vs. role), the CQRS dispatcher, through the real HTTP pipeline
/// (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class RestaurantEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public RestaurantEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfig_Anonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/restaurant/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RestaurantConfigDto>();
        dto.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOpeningHours_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var command = new UpdateOpeningHoursCommand(new OpeningHoursDto(new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>()));

        var response = await client.PutAsJsonAsync("/api/restaurant/opening-hours", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOpeningHours_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var command = new UpdateOpeningHoursCommand(new OpeningHoursDto(new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>()));

        var response = await client.PutAsJsonAsync("/api/restaurant/opening-hours", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOpeningHours_WithAdminRole_UpdatesConfig()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>
        {
            [DayOfWeek.Friday] = new List<TimeRangeDto> { new(new TimeOnly(11, 0), new TimeOnly(23, 0)) },
        };
        var command = new UpdateOpeningHoursCommand(new OpeningHoursDto(schedule));

        var response = await client.PutAsJsonAsync("/api/restaurant/opening-hours", command);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configResponse = await client.GetAsync("/api/restaurant/config");
        var dto = await configResponse.Content.ReadFromJsonAsync<RestaurantConfigDto>();
        dto!.OpeningHours.Schedule[DayOfWeek.Friday].Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateDeliveryArea_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var command = new UpdateDeliveryAreaCommand(50.05, 19.95, 8);

        var response = await client.PutAsJsonAsync("/api/restaurant/delivery-area", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDeliveryArea_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var command = new UpdateDeliveryAreaCommand(50.05, 19.95, 8);

        var response = await client.PutAsJsonAsync("/api/restaurant/delivery-area", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDeliveryArea_WithAdminRole_UpdatesConfig()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var command = new UpdateDeliveryAreaCommand(50.05, 19.95, 8);

        var response = await client.PutAsJsonAsync("/api/restaurant/delivery-area", command);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configResponse = await client.GetAsync("/api/restaurant/config");
        var dto = await configResponse.Content.ReadFromJsonAsync<RestaurantConfigDto>();
        dto!.DeliveryRadiusKm.Should().Be(8);
    }

    [Fact]
    public async Task UpdateOrderingThresholds_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var command = new UpdateOrderingThresholdsCommand(
            new MoneyDto(30m, "PLN"),
            new MoneyDto(80m, "PLN"),
            new MoneyDto(12m, "PLN"));

        var response = await client.PutAsJsonAsync("/api/restaurant/ordering-thresholds", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOrderingThresholds_WithEmployeeRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var command = new UpdateOrderingThresholdsCommand(
            new MoneyDto(30m, "PLN"),
            new MoneyDto(80m, "PLN"),
            new MoneyDto(12m, "PLN"));

        var response = await client.PutAsJsonAsync("/api/restaurant/ordering-thresholds", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrderingThresholds_WithAdminRole_UpdatesConfig()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var command = new UpdateOrderingThresholdsCommand(
            new MoneyDto(30m, "PLN"),
            new MoneyDto(80m, "PLN"),
            new MoneyDto(12m, "PLN"));

        var response = await client.PutAsJsonAsync("/api/restaurant/ordering-thresholds", command);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configResponse = await client.GetAsync("/api/restaurant/config");
        var dto = await configResponse.Content.ReadFromJsonAsync<RestaurantConfigDto>();
        dto!.DeliveryFee.Amount.Should().Be(12m);
    }

    [Fact]
    public async Task ToggleAcceptingOrders_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/restaurant/accepting-orders", new ToggleAcceptingOrdersCommand(false));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleAcceptingOrders_WithEmployeeRole_UpdatesConfig()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await client.PostAsJsonAsync("/api/restaurant/accepting-orders", new ToggleAcceptingOrdersCommand(false));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configResponse = await client.GetAsync("/api/restaurant/config");
        var dto = await configResponse.Content.ReadFromJsonAsync<RestaurantConfigDto>();
        dto!.IsAcceptingOrders.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleAcceptingOrders_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/restaurant/accepting-orders", new ToggleAcceptingOrdersCommand(true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
