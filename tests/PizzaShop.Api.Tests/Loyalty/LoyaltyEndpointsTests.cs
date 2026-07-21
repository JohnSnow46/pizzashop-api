using System.Net;
using System.Net.Http.Json;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Loyalty.Dtos;
using FluentAssertions;

namespace PizzaShop.Api.Tests.Loyalty;

/// <summary>
/// End-to-end tests for <c>/api/loyalty</c> (api-layer.md 6.8): authorization (only
/// <c>Customer</c> may call it) and the happy path, through the real HTTP pipeline
/// (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class LoyaltyEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public LoyaltyEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBalance_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/loyalty/balance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalance_TokenInQueryStringWithoutAuthHeader_ReturnsUnauthorized()
    {
        // The "access_token" query string carve-out in Program.cs is scoped to
        // /hubs/order-tracking only (ADR-0028). A REST endpoint like this one must not honor a
        // valid JWT passed as ?access_token=... without an Authorization header — otherwise the
        // carve-out would leak into every endpoint and weaken normal REST authorization.
        var token = await AuthTestHelper.CreateCustomerTokenAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/loyalty/balance?access_token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalance_Staff_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await client.GetAsync("/api/loyalty/balance");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBalance_Customer_ReturnsOwnZeroBalance()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.GetAsync("/api/loyalty/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = await response.Content.ReadFromJsonAsync<LoyaltyBalanceDto>();
        balance!.PointsBalance.Should().Be(0);
        balance.Transactions.Should().BeEmpty();
    }
}
