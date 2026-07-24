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
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Orders;

/// <summary>
/// End-to-end tests for <c>/api/orders</c> (api-layer.md 6.6): routing, JWT authorization
/// (anonymous/own/staff), ownership scoping, the status-transition graph, and the
/// route-wins-over-body id rule (api-layer.md 1.1/ADR-0030) for <c>accept</c> and
/// <c>estimated-ready-at</c> — through the real HTTP pipeline (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class OrdersEndpointsTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Matches FakeGeocodingService's default branch: resolves to the test restaurant's own
    // coordinate, always within its delivery radius.
    private static readonly AddressDto WithinRadiusAddress = new("Testowa", "1", "Kraków", "30-001");
    private static readonly AddressDto OutsideRadiusAddress = new(FakeGeocodingService.OutsideRadiusStreet, "1", "Warszawa", "00-001");

    private readonly ApiTestFactory _factory;

    public OrdersEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static ContactDetailsDto Contact() => new("Jan Kowalski", "123456789", "jan@example.com");

    private async Task<Guid> CreateDrinkMenuItemAsync(string name)
    {
        var adminClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var command = new CreateMenuItemCommand(
            name, MenuCategory.Drink, new MoneyDto(9.99m, "PLN"), "Cold drink", null,
            Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<MenuItemVariantInputDto>());

        var response = await adminClient.PostAsJsonAsync("/api/menu", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private CreateOrderCommand PickupOrderCommand(Guid menuItemId, PaymentMethod paymentMethod = PaymentMethod.OnPickup) =>
        new(
            Contact(),
            FulfillmentType.Pickup,
            null,
            new List<CreateOrderItemDto> { new(menuItemId, null, 1, Array.Empty<Guid>()) },
            null,
            paymentMethod);

    private CreateOrderCommand DeliveryOrderCommand(Guid menuItemId, AddressDto address, PaymentMethod paymentMethod = PaymentMethod.OnPickup) =>
        new(
            Contact(),
            FulfillmentType.Delivery,
            address,
            new List<CreateOrderItemDto> { new(menuItemId, null, 1, Array.Empty<Guid>()) },
            null,
            paymentMethod);

    // ---- POST /check-delivery ----

    [Fact]
    public async Task CheckDelivery_WithinRadius_Anonymous_ReturnsAvailable()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders/check-delivery", new CheckDeliveryAvailabilityQuery(WithinRadiusAddress));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<DeliveryAvailabilityDto>(JsonOptions);
        dto!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDelivery_OutsideRadius_ReturnsUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders/check-delivery", new CheckDeliveryAvailabilityQuery(OutsideRadiusAddress));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<DeliveryAvailabilityDto>(JsonOptions);
        dto!.IsAvailable.Should().BeFalse();
    }

    // ---- POST / ----

    [Fact]
    public async Task Create_AsGuest_ReturnsGuestTrackingTokenAndNoCustomerId()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Guest");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", PickupOrderCommand(menuItemId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResultDto>(JsonOptions);
        result!.GuestTrackingToken.Should().NotBeNull();

        var orderResponse = await client.GetAsync($"/api/orders/track/{result.GuestTrackingToken}");
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsNoGuestTrackingTokenAndOwnedOrder()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Customer");
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/orders", PickupOrderCommand(menuItemId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResultDto>(JsonOptions);
        result!.GuestTrackingToken.Should().BeNull();

        var orderResponse = await client.GetAsync($"/api/orders/{result.OrderId}");
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.CustomerId.Should().NotBeNull();
    }

    // ---- GET /{id} ----

    [Fact]
    public async Task GetById_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwningCustomer_ReturnsOrder()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Owner");
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(client, PickupOrderCommand(menuItemId));

        var response = await client.GetAsync($"/api/orders/{created.OrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonOwningCustomer_ReturnsNotFound()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-NonOwner");
        var ownerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(ownerClient, PickupOrderCommand(menuItemId));

        var otherClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var response = await otherClient.GetAsync($"/api/orders/{created.OrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Staff_ReturnsAnyOrder()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Staff");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var response = await staffClient.GetAsync($"/api/orders/{created.OrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- GET /mine ----

    [Fact]
    public async Task GetMine_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_WithStaffRole_ReturnsForbidden()
    {
        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await staffClient.GetAsync("/api/orders/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMine_TwoCustomers_EachSeesOnlyOwnOrders()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-MineScoping");

        var customerAClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var createdA = await CreateOrderAsync(customerAClient, PickupOrderCommand(menuItemId));

        var customerBClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var createdB = await CreateOrderAsync(customerBClient, PickupOrderCommand(menuItemId));

        var responseA = await customerAClient.GetAsync("/api/orders/mine");

        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var ordersA = await responseA.Content.ReadFromJsonAsync<List<OrderSummaryDto>>(JsonOptions);
        ordersA.Should().ContainSingle(o => o.Id == createdA.OrderId);
        ordersA.Should().NotContain(o => o.Id == createdB.OrderId);
    }

    // ---- GET /track/{trackingToken} ----

    [Fact]
    public async Task GetByTrackingToken_InvalidToken_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/track/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- GET /queue ----

    [Fact]
    public async Task GetQueue_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetQueue_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.GetAsync("/api/orders/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetQueue_WithStaffRole_ContainsPendingOrder()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Queue");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var response = await staffClient.GetAsync("/api/orders/queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var queue = await response.Content.ReadFromJsonAsync<List<OrderDto>>(JsonOptions);
        queue.Should().Contain(o => o.Id == created.OrderId);
    }

    // ---- Status transitions ----

    [Fact]
    public async Task Accept_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accept_WithCustomerRole_ReturnsForbidden()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-AcceptForbidden");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var response = await customerClient.PostAsJsonAsync($"/api/orders/{created.OrderId}/accept", new AcceptOrderCommand(created.OrderId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// All staff-only status-transition endpoints (<c>reject</c>, <c>start-preparation</c>,
    /// <c>mark-ready</c>, <c>start-delivery</c>, <c>complete</c>) carry
    /// <c>[Authorize(Roles = AuthRoles.Staff)]</c> in <c>OrdersController</c> — mirrors
    /// <see cref="Accept_WithCustomerRole_ReturnsForbidden"/> for the rest of the transition graph.
    /// </summary>
    [Theory]
    [InlineData("reject")]
    [InlineData("start-preparation")]
    [InlineData("mark-ready")]
    [InlineData("start-delivery")]
    [InlineData("complete")]
    public async Task StatusTransition_WithCustomerRole_ReturnsForbidden(string action)
    {
        var menuItemId = await CreateDrinkMenuItemAsync($"Cola-{action}-Forbidden");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var response = await customerClient.PostAsync($"/api/orders/{created.OrderId}/{action}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// ADR-0030 (api-layer.md 1.1): the body's <c>OrderId</c> is a mismatched, unrelated guid —
    /// the route id must still be the one the command actually acts on.
    /// </summary>
    [Fact]
    public async Task Accept_WithMismatchedBodyOrderId_UsesRouteId()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-AcceptRoute");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var mismatchedBody = new AcceptOrderCommand(Guid.NewGuid());

        var response = await staffClient.PostAsJsonAsync($"/api/orders/{created.OrderId}/accept", mismatchedBody);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var orderResponse = await staffClient.GetAsync($"/api/orders/{created.OrderId}");
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.Status.Should().Be(OrderStatus.Accepted);
    }

    [Fact]
    public async Task Reject_WithStaffRole_TransitionsToRejected()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Reject");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var response = await staffClient.PostAsync($"/api/orders/{created.OrderId}/reject", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var orderResponse = await staffClient.GetAsync($"/api/orders/{created.OrderId}");
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.Status.Should().Be(OrderStatus.Rejected);
    }

    [Fact]
    public async Task FullDeliveryLifecycle_TransitionsThroughToCompleted()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-Lifecycle");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, DeliveryOrderCommand(menuItemId, WithinRadiusAddress));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var orderUrl = $"/api/orders/{created.OrderId}";

        (await staffClient.PostAsJsonAsync($"{orderUrl}/accept", new AcceptOrderCommand(created.OrderId)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await staffClient.PostAsync($"{orderUrl}/start-preparation", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await staffClient.PostAsync($"{orderUrl}/mark-ready", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await staffClient.PostAsync($"{orderUrl}/start-delivery", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await staffClient.PostAsync($"{orderUrl}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var orderResponse = await staffClient.GetAsync(orderUrl);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.Status.Should().Be(OrderStatus.Completed);
    }

    // ---- PUT /{id}/estimated-ready-at ----

    [Fact]
    public async Task SetEstimatedReadyAt_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/orders/{Guid.NewGuid()}/estimated-ready-at",
            new SetEstimatedReadyAtCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(30)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetEstimatedReadyAt_WithCustomerRole_ReturnsForbidden()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-EtaForbidden");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var response = await customerClient.PutAsJsonAsync(
            $"/api/orders/{created.OrderId}/estimated-ready-at",
            new SetEstimatedReadyAtCommand(created.OrderId, DateTimeOffset.UtcNow.AddMinutes(30)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>ADR-0030 (api-layer.md 1.1): route id wins over the body's mismatched <c>OrderId</c>.</summary>
    [Fact]
    public async Task SetEstimatedReadyAt_WithMismatchedBodyOrderId_UsesRouteId()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-EtaRoute");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        await staffClient.PostAsJsonAsync($"/api/orders/{created.OrderId}/accept", new AcceptOrderCommand(created.OrderId));

        var eta = DateTimeOffset.UtcNow.AddMinutes(20);
        var mismatchedBody = new SetEstimatedReadyAtCommand(Guid.NewGuid(), eta);

        var response = await staffClient.PutAsJsonAsync($"/api/orders/{created.OrderId}/estimated-ready-at", mismatchedBody);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var orderResponse = await staffClient.GetAsync($"/api/orders/{created.OrderId}");
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order!.EstimatedReadyAt.Should().BeCloseTo(eta, TimeSpan.FromSeconds(1));
    }

    // ---- POST /{id}/cancel ----

    [Fact]
    public async Task Cancel_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancel_OwningCustomer_BeforeAcceptance_Succeeds()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-CancelOwner");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var response = await customerClient.PostAsync($"/api/orders/{created.OrderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_NonOwningCustomer_ReturnsNotFound()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-CancelNonOwner");
        var ownerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(ownerClient, PickupOrderCommand(menuItemId));

        var otherClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var response = await otherClient.PostAsync($"/api/orders/{created.OrderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_OwningCustomer_AfterAcceptance_ReturnsForbidden()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-CancelAfterAccept");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        await staffClient.PostAsJsonAsync($"/api/orders/{created.OrderId}/accept", new AcceptOrderCommand(created.OrderId));

        var response = await customerClient.PostAsync($"/api/orders/{created.OrderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Staff_AfterAcceptance_Succeeds()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-CancelStaff");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, PickupOrderCommand(menuItemId));

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        await staffClient.PostAsJsonAsync($"/api/orders/{created.OrderId}/accept", new AcceptOrderCommand(created.OrderId));

        var response = await staffClient.PostAsync($"/api/orders/{created.OrderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<CreateOrderResultDto> CreateOrderAsync(HttpClient client, CreateOrderCommand command)
    {
        var response = await client.PostAsJsonAsync("/api/orders", command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateOrderResultDto>(JsonOptions))!;
    }
}
