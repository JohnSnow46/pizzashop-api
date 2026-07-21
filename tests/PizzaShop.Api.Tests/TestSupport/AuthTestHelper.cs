using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// Shared helpers for Iteration 2+ Api tests to obtain an authenticated <see cref="HttpClient"/>
/// for a given role, reusing the real <c>/api/auth</c> endpoints (register/login/staff) rather
/// than minting JWTs by hand — this exercises the same pipeline
/// <see cref="Auth.AuthEndpointsTests"/> already covers.
/// </summary>
public static class AuthTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string Password = "Password123";

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@pizzashop.test";

    /// <summary>Registers a fresh customer and returns an <see cref="HttpClient"/> authenticated as them.</summary>
    public static async Task<HttpClient> CreateCustomerClientAsync(ApiTestFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCustomerCommand(UniqueEmail("customer"), Password, "Jan Kowalski", null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }

    /// <summary>
    /// Creates a fresh staff account with the given <paramref name="role"/> (via the seeded
    /// SuperAdmin) and returns an <see cref="HttpClient"/> authenticated as that account.
    /// </summary>
    public static async Task<HttpClient> CreateStaffClientAsync(ApiTestFactory factory, UserRole role)
    {
        var bootstrapClient = factory.CreateClient();
        var superAdminToken = await LoginAsync(bootstrapClient, ApiTestFactory.SuperAdminEmail, ApiTestFactory.SuperAdminPassword);
        bootstrapClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superAdminToken);

        var email = UniqueEmail(role.ToString().ToLowerInvariant());
        var staffResponse = await bootstrapClient.PostAsJsonAsync(
            "/api/auth/staff",
            new RegisterStaffAccountCommand(email, Password, role));
        staffResponse.EnsureSuccessStatusCode();

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        return result!.Token;
    }
}
