using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using PizzaShop.Application.Payments.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Payments;

/// <summary>
/// End-to-end tests for <c>/api/payments</c> (api-layer.md 6.7): routing, JWT authorization,
/// ownership scoping, and the anonymous PayU webhook with raw-body signature verification
/// (api-layer.md 7) — through the real HTTP pipeline (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class PaymentsEndpointsTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiTestFactory _factory;

    public PaymentsEndpointsTests(ApiTestFactory factory)
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

    private async Task<CreateOrderResultDto> CreateOrderAsync(HttpClient client, Guid menuItemId, PaymentMethod paymentMethod)
    {
        var command = new CreateOrderCommand(
            Contact(),
            FulfillmentType.Pickup,
            null,
            new List<CreateOrderItemDto> { new(menuItemId, null, 1, Array.Empty<Guid>()) },
            null,
            paymentMethod);

        var response = await client.PostAsJsonAsync("/api/orders", command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateOrderResultDto>(JsonOptions))!;
    }

    // ---- POST /orders/{id}/initialize ----

    [Fact]
    public async Task Initialize_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/payments/orders/{Guid.NewGuid()}/initialize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Initialize_OwningCustomer_OnlineOrder_ReturnsRedirectUrl()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-InitOwner");
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(client, menuItemId, PaymentMethod.Online);

        var response = await client.PostAsync($"/api/payments/orders/{created.OrderId}/initialize", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InitializePaymentResultDto>(JsonOptions);
        result!.RedirectUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Initialize_NonOwningCustomer_ReturnsNotFound()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-InitNonOwner");
        var ownerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(ownerClient, menuItemId, PaymentMethod.Online);

        var otherClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var response = await otherClient.PostAsync($"/api/payments/orders/{created.OrderId}/initialize", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Initialize_OnPickupOrder_ReturnsConflict()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-InitOnPickup");
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(client, menuItemId, PaymentMethod.OnPickup);

        var response = await client.PostAsync($"/api/payments/orders/{created.OrderId}/initialize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- GET /orders/{id}/status ----

    [Fact]
    public async Task GetStatus_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/payments/orders/{Guid.NewGuid()}/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatus_OwningCustomer_ReturnsPending()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-StatusOwner");
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(client, menuItemId, PaymentMethod.Online);

        var response = await client.GetAsync($"/api/payments/orders/{created.OrderId}/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<PaymentStatusDto>(JsonOptions);
        status!.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetStatus_NonOwningCustomer_ReturnsNotFound()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-StatusNonOwner");
        var ownerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(ownerClient, menuItemId, PaymentMethod.Online);

        var otherClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var response = await otherClient.GetAsync($"/api/payments/orders/{created.OrderId}/status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStatus_Staff_ReturnsOk()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-StatusStaff");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, menuItemId, PaymentMethod.Online);

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var response = await staffClient.GetAsync($"/api/payments/orders/{created.OrderId}/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- POST /payu/webhook ----

    [Fact]
    public async Task Webhook_ValidSignature_NoAuthorizationHeader_MarksOrderPaid()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-WebhookPaid");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, menuItemId, PaymentMethod.Online);

        var anonymousClient = _factory.CreateClient(); // No Authorization header — the webhook must not need one.
        var response = await PostWebhookAsync(anonymousClient, created.OrderId, FakePaymentGateway.ValidSignature);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await customerClient.GetAsync($"/api/payments/orders/{created.OrderId}/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<PaymentStatusDto>(JsonOptions);
        status!.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Webhook_DuplicateValidNotification_StaysIdempotent()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Pizza-WebhookIdempotent");
        var customerClient = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var created = await CreateOrderAsync(customerClient, menuItemId, PaymentMethod.Online);

        var anonymousClient = _factory.CreateClient();
        var first = await PostWebhookAsync(anonymousClient, created.OrderId, FakePaymentGateway.ValidSignature);
        var second = await PostWebhookAsync(anonymousClient, created.OrderId, FakePaymentGateway.ValidSignature);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await customerClient.GetAsync($"/api/payments/orders/{created.OrderId}/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<PaymentStatusDto>(JsonOptions);
        status!.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await PostWebhookAsync(client, Guid.NewGuid(), "not-the-right-signature");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_MissingSignatureHeader_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var body = FakePaymentGateway.BuildNotificationBody(Guid.NewGuid(), "any-ref", PaymentStatus.Paid);

        var response = await client.PostAsync(
            "/api/payments/payu/webhook",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, Guid orderId, string signature)
    {
        var body = FakePaymentGateway.BuildNotificationBody(orderId, $"payu-ref-{orderId:N}", PaymentStatus.Paid);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add(FakePaymentGateway.SignatureHeaderName, signature);

        return client.PostAsync("/api/payments/payu/webhook", content);
    }
}
