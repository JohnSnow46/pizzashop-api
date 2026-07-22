using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Realtime;

/// <summary>
/// Full-pipeline integration test for <see cref="PizzaShop.Api.Realtime.OrderTrackingHub"/>
/// (api-layer.md 8.1, ADR-0028). <see cref="OrderTrackingHubTests"/> mocks
/// <see cref="PizzaShop.Application.Common.Messaging.IDispatcher"/> to unit-test the hub's own
/// subscribe/guard logic in isolation — that leaves the real WebSocket/long-polling JWT
/// authentication (the "access_token" query-string carve-out in Program.cs) and
/// <c>HttpContextCurrentUser</c>'s ability to resolve the caller's identity from *inside a hub
/// method invocation* completely unexercised. A mocked <c>HubCallerContext</c> can't catch a
/// regression there: if <see cref="IHttpContextAccessor"/> ever failed to see the ambient
/// <c>HttpContext</c> during a real hub call, <c>SubscribeToOrder</c> would silently add nobody to
/// the group — indistinguishable from "order doesn't exist", so no mock-based test would notice.
///
/// This test instead drives a real <see cref="HubConnection"/> against the in-memory
/// <c>TestServer</c> (long polling — <c>TestServer</c> has no real socket to upgrade to
/// WebSockets, but exercises the exact same <c>JwtBearerEvents.OnMessageReceived</c> code path
/// over plain HTTP requests), through a real order created via the HTTP API, and a real staff
/// status transition (<c>AcceptOrderCommandHandler</c> -&gt;
/// <see cref="PizzaShop.Api.Realtime.SignalROrderNotifier"/>) that must arrive as an
/// <c>OrderStatusChanged</c> event.
/// </summary>
public sealed class OrderTrackingHubIntegrationTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);

    private readonly ApiTestFactory _factory;

    public OrderTrackingHubIntegrationTests(ApiTestFactory factory)
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

    private static async Task<Guid> CreateOrderAsync(HttpClient client, Guid menuItemId)
    {
        var command = new CreateOrderCommand(
            Contact(),
            FulfillmentType.Pickup,
            null,
            new List<CreateOrderItemDto> { new(menuItemId, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var response = await client.PostAsJsonAsync("/api/orders", command);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResultDto>(JsonOptions);
        return result!.OrderId;
    }

    /// <summary>
    /// Mirrors the production wiring in Program.cs: the same JWT the customer would normally send
    /// as an Authorization header is instead handed to SignalR's <c>AccessTokenProvider</c>, which
    /// puts it on the "access_token" query string of every request the connection makes — the
    /// exact channel <c>JwtBearerEvents.OnMessageReceived</c> reads for <c>/hubs/order-tracking</c>.
    /// </summary>
    private HubConnection BuildConnection(string accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/order-tracking"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
    }

    /// <summary>
    /// Program.cs doesn't configure a JSON hub protocol, so SignalR's default
    /// <c>System.Text.Json</c> settings apply — enums may serialize as their numeric value rather
    /// than the string name Api controllers use (they opt into <c>JsonStringEnumConverter</c>
    /// explicitly). Accept either so this assertion doesn't depend on that server-side default.
    /// </summary>
    private static bool StatusEquals(JsonElement payload, OrderStatus expected)
    {
        var status = payload.GetProperty("status");
        return status.ValueKind == JsonValueKind.Number
            ? status.GetInt32() == (int)expected
            : string.Equals(status.GetString(), expected.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubscribeToOrder_OwningCustomer_ReceivesOrderStatusChanged()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-HubOwner");
        var (ownerClient, ownerToken) = await AuthTestHelper.CreateCustomerAsync(_factory);
        var orderId = await CreateOrderAsync(ownerClient, menuItemId);

        await using var connection = BuildConnection(ownerToken);
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>("OrderStatusChanged", payload => received.TrySetResult(payload));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToOrder", orderId);

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var acceptResponse = await staffClient.PostAsJsonAsync($"/api/orders/{orderId}/accept", new AcceptOrderCommand(orderId));
        acceptResponse.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(received.Task, Task.Delay(EventTimeout));
        completed.Should().Be(
            received.Task,
            "the owning customer's hub connection, authenticated via the access_token query string, should have joined the order's group and received the push");

        var payload = await received.Task;
        payload.GetProperty("orderId").GetGuid().Should().Be(orderId);
        StatusEquals(payload, OrderStatus.Accepted).Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeToOrder_NonOwningCustomer_NeverReceivesOrderStatusChanged()
    {
        var menuItemId = await CreateDrinkMenuItemAsync("Cola-HubNonOwner");
        var (ownerClient, _) = await AuthTestHelper.CreateCustomerAsync(_factory);
        var orderId = await CreateOrderAsync(ownerClient, menuItemId);

        var (_, otherToken) = await AuthTestHelper.CreateCustomerAsync(_factory);

        await using var connection = BuildConnection(otherToken);
        var receivedCount = 0;
        connection.On<JsonElement>("OrderStatusChanged", _ => Interlocked.Increment(ref receivedCount));

        await connection.StartAsync();

        // Same call the owner makes in the sibling test — GetOrderByIdQueryHandler's ownership
        // check (not the hub itself) is what must silently refuse to add this connection to the
        // order's group, exactly like an unknown order id would.
        await connection.InvokeAsync("SubscribeToOrder", orderId);

        var staffClient = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);
        var acceptResponse = await staffClient.PostAsJsonAsync($"/api/orders/{orderId}/accept", new AcceptOrderCommand(orderId));
        acceptResponse.EnsureSuccessStatusCode();

        // There's no positive signal to await for an absence — give the push pipeline a generous
        // window to have delivered the event if it were (incorrectly) going to.
        await Task.Delay(TimeSpan.FromSeconds(2));
        receivedCount.Should().Be(0);
    }
}
