using System.Net.Http.Headers;
using FluentAssertions;
using PizzaShop.Api.Tests.TestSupport;

namespace PizzaShop.Api.Tests.Cors;

/// <summary>
/// Verifies the "frontend" CORS policy (Program.cs, ADR-0035): an origin present in
/// <c>Cors:Origins</c> gets echoed back via <c>Access-Control-Allow-Origin</c>, an origin
/// outside the configured list does not.
/// </summary>
public sealed class CorsPolicyTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public CorsPolicyTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMenu_FromAllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/menu");
        request.Headers.Add("Origin", ApiTestFactory.AllowedCorsOrigin);

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be(ApiTestFactory.AllowedCorsOrigin);
    }

    [Fact]
    public async Task GetMenu_FromDisallowedOrigin_DoesNotReturnAccessControlAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/menu");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out _).Should().BeFalse();
    }
}
