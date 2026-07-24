using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Application.Promotions.Queries;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.Promotions;

/// <summary>
/// End-to-end tests for <c>/api/promotions</c> (api-layer.md 6.5): routing, JWT authorization
/// (anonymous vs. role), the CQRS dispatcher and FluentValidation, through the real HTTP
/// pipeline (<see cref="ApiTestFactory"/>).
/// </summary>
public sealed class PromotionsEndpointsTests : IClassFixture<ApiTestFactory>
{
    // Program.cs serializes controller responses with a JsonStringEnumConverter (enums as
    // strings) — deserializing DTOs that carry an enum (PromotionDto.Type) needs the same
    // converter on the read side.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiTestFactory _factory;

    public PromotionsEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static CreatePromotionCommand ValidCommand(string code) =>
        new(
            "10 PLN off",
            PromotionType.FixedAmount,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1),
            10m,
            code,
            null,
            null);

    [Fact]
    public async Task Validate_Anonymous_UnknownCode_ReturnsNotQualified()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/promotions/validate",
            new ValidatePromotionCodeQuery("NOPE", new MoneyDto(50m, "PLN"), new MoneyDto(5m, "PLN")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PromotionDiscountPreviewDto>();
        dto!.IsQualified.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/promotions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);

        var response = await client.GetAsync("/api/promotions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var code = $"SAVE-{Guid.NewGuid():N}".ToUpperInvariant();

        var response = await client.PostAsJsonAsync("/api/promotions", ValidCommand(code));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var code = $"SAVE-{Guid.NewGuid():N}".ToUpperInvariant();

        var response = await client.PostAsJsonAsync("/api/promotions", ValidCommand(code));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAdminRole_CreatesPromotion_ThenListedAndValidatable()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var code = $"SAVE-{Guid.NewGuid():N}".ToUpperInvariant();

        var createResponse = await client.PostAsJsonAsync("/api/promotions", ValidCommand(code));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();

        var listResponse = await client.GetAsync("/api/promotions");
        var promotions = await listResponse.Content.ReadFromJsonAsync<List<PromotionDto>>(JsonOptions);
        promotions.Should().Contain(p => p.Id == id);

        var validateResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/promotions/validate",
            new ValidatePromotionCodeQuery(code, new MoneyDto(50m, "PLN"), new MoneyDto(5m, "PLN")));
        var preview = await validateResponse.Content.ReadFromJsonAsync<PromotionDiscountPreviewDto>();
        preview!.IsQualified.Should().BeTrue();
        preview.DiscountAmount!.Amount.Should().Be(10m);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync($"/api/promotions/{id}", new UpdatePromotionCommand(id, true));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync($"/api/promotions/{id}", new UpdatePromotionCommand(id, true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithAdminRole_DeactivatesPromotion()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var code = $"OFF-{Guid.NewGuid():N}".ToUpperInvariant();
        var createResponse = await client.PostAsJsonAsync("/api/promotions", ValidCommand(code));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateCommand = new UpdatePromotionCommand(id, false);
        var updateResponse = await client.PutAsJsonAsync($"/api/promotions/{id}", updateCommand);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var validateResponse = await client.PostAsJsonAsync(
            "/api/promotions/validate",
            new ValidatePromotionCodeQuery(code, new MoneyDto(50m, "PLN"), new MoneyDto(5m, "PLN")));
        var preview = await validateResponse.Content.ReadFromJsonAsync<PromotionDiscountPreviewDto>();
        preview!.IsQualified.Should().BeFalse();
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var id = Guid.NewGuid();

        var response = await client.PutAsJsonAsync($"/api/promotions/{id}", new UpdatePromotionCommand(id, true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.PatchAsync($"/api/promotions/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_WithCustomerRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateCustomerClientAsync(_factory);
        var id = Guid.NewGuid();

        var response = await client.PatchAsync($"/api/promotions/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_WithAdminRole_DeactivatesPromotion()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var code = $"DEACT-{Guid.NewGuid():N}".ToUpperInvariant();
        var createResponse = await client.PostAsJsonAsync("/api/promotions", ValidCommand(code));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deactivateResponse = await client.PatchAsync($"/api/promotions/{id}/deactivate", null);

        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var validateResponse = await client.PostAsJsonAsync(
            "/api/promotions/validate",
            new ValidatePromotionCodeQuery(code, new MoneyDto(50m, "PLN"), new MoneyDto(5m, "PLN")));
        var preview = await validateResponse.Content.ReadFromJsonAsync<PromotionDiscountPreviewDto>();
        preview!.IsQualified.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_UnknownId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);
        var id = Guid.NewGuid();

        var response = await client.PatchAsync($"/api/promotions/{id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
