using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PizzaShop.Domain.ValueObjects;
using PizzaShop.Infrastructure.Geocoding;

namespace PizzaShop.Infrastructure.Tests.Geocoding;

/// <summary>
/// Unit tests for <see cref="NominatimGeocodingService"/> against a stubbed
/// <see cref="HttpMessageHandler"/> — no real requests to OpenStreetMap (ADR-0023).
/// </summary>
public sealed class NominatimGeocodingServiceTests
{
    private static readonly Address SampleAddress = new("Main St", "1", "Warsaw", "00-001");

    [Fact]
    public async Task GeocodeAsync_ResultFound_ReturnsCoordinate()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            "[{\"lat\":\"52.2297\",\"lon\":\"21.0122\"}]");

        var service = CreateService(handler);

        var result = await service.GeocodeAsync(SampleAddress, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Latitude.Should().BeApproximately(52.2297, 0.0001);
        result.Longitude.Should().BeApproximately(21.0122, 0.0001);
    }

    [Fact]
    public async Task GeocodeAsync_NoResults_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var service = CreateService(handler);

        var result = await service.GeocodeAsync(SampleAddress, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAsync_SendsRequiredUserAgentHeader()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var service = CreateService(handler, userAgent: "PizzaShop-Test/1.0");

        await service.GeocodeAsync(SampleAddress, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.UserAgent.ToString().Should().Contain("PizzaShop-Test/1.0");
    }

    private static NominatimGeocodingService CreateService(HttpMessageHandler handler, string userAgent = "PizzaShop-Test/1.0")
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new GeocodingOptions
        {
            BaseUrl = "https://nominatim.example.test",
            UserAgent = userAgent,
            TimeoutSeconds = 5,
        });

        return new NominatimGeocodingService(httpClient, options);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody),
            };

            return Task.FromResult(response);
        }
    }
}
