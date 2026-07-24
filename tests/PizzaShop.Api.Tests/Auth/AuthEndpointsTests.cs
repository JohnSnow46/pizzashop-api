using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Api.Auth;
using PizzaShop.Api.Tests.TestSupport;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Api.Tests.Auth;

/// <summary>
/// End-to-end tests for <c>/api/auth</c> through the real HTTP pipeline (api-layer.md 11 step
/// 12): routing, JWT auth/authorization, the CQRS dispatcher, FluentValidation, and
/// <c>PizzaShop.Api.Middleware.ExceptionHandler</c>. Persistence is faked in-memory
/// (<see cref="ApiTestFactory"/>) — this is not a persistence test.
/// </summary>
public sealed class AuthEndpointsTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiTestFactory _factory;

    public AuthEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@pizzashop.test";

    [Fact]
    public async Task Register_Login_Me_FullFlow_ReturnsAuthenticatedCustomer()
    {
        var client = CreateClient();
        var email = UniqueEmail("customer");

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCustomerCommand(email, "Password123", "Jan Kowalski", "123456789"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        registerResult.Should().NotBeNull();
        registerResult!.Role.Should().Be(UserRole.Customer);
        registerResult.CustomerId.Should().NotBeNull();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "Password123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrWhiteSpace();
        loginResult.CustomerId.Should().Be(registerResult.CustomerId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        var meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
        me.Should().NotBeNull();
        me!.UserAccountId.Should().Be(registerResult.UserAccountId);
        me.Role.Should().Be(UserRole.Customer);
        me.CustomerId.Should().Be(registerResult.CustomerId);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterStaff_WithoutAdminRole_ReturnsForbidden()
    {
        var client = CreateClient();
        var customerEmail = UniqueEmail("plain-customer");
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCustomerCommand(customerEmail, "Password123", "Jan Kowalski", null));
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.Token);

        var response = await client.PostAsJsonAsync(
            "/api/auth/staff",
            new RegisterStaffAccountCommand(UniqueEmail("staff"), "Password123", UserRole.Employee));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterStaff_WithSeededSuperAdmin_CreatesEmployeeAccount()
    {
        var client = CreateClient();
        var superAdminLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(ApiTestFactory.SuperAdminEmail, ApiTestFactory.SuperAdminPassword));
        superAdminLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var superAdminResult = await superAdminLogin.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superAdminResult!.Token);

        var response = await client.PostAsJsonAsync(
            "/api/auth/staff",
            new RegisterStaffAccountCommand(UniqueEmail("staff"), "Password123", UserRole.Employee));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        result!.Role.Should().Be(UserRole.Employee);
        result.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task GetStaff_WithoutAdminRole_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.Employee);

        var response = await client.GetAsync("/api/auth/staff");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStaff_WithRestaurantAdminRole_ReturnsOk()
    {
        var client = await AuthTestHelper.CreateStaffClientAsync(_factory, UserRole.RestaurantAdmin);

        var response = await client.GetAsync("/api/auth/staff");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserAccountDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Should().NotContain(a => a.Role == UserRole.Customer);
    }

    [Fact]
    public async Task GetStaff_WithSuperAdminRole_ReturnsOk()
    {
        var client = CreateClient();
        var superAdminToken = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(ApiTestFactory.SuperAdminEmail, ApiTestFactory.SuperAdminPassword));
        var superAdminResult = await superAdminToken.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superAdminResult!.Token);

        var response = await client.GetAsync("/api/auth/staff");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = CreateClient();
        var email = UniqueEmail("duplicate");
        var command = new RegisterCustomerCommand(email, "Password123", "Jan Kowalski", null);

        var first = await client.PostAsJsonAsync("/api/auth/register", command);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/auth/register", command);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();
        var email = UniqueEmail("badlogin");
        await client.PostAsJsonAsync("/api/auth/register", new RegisterCustomerCommand(email, "Password123", "Jan Kowalski", null));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "WrongPassword1"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(UniqueEmail("nobody"), "Password123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidShape_ReturnsBadRequestWithValidationErrors()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCustomerCommand("not-an-email", "short", "", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey(nameof(RegisterCustomerCommand.Email));
    }

    [Fact]
    public async Task UnhandledException_ReturnsInternalServerErrorProblemDetails()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(InMemoryUserAccountRepository.PoisonEmail, "Password123"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        // A 500 must never leak the underlying exception message (ExceptionHandler contract).
        problem.Detail.Should().BeNull();
    }
}
