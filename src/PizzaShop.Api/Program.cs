using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PizzaShop.Api;
using PizzaShop.Api.Auth;
using PizzaShop.Api.Middleware;
using PizzaShop.Application;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Infrastructure;
using PizzaShop.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Application layer (dispatcher, validators, handlers — ADR-0012).
builder.Services.AddApplication();

// 2. Infrastructure layer (DbContext, repositories, UoW, PayU, geocoding, clock, loyalty
// policy — ADR-0024, infrastructure-layer.md 8).
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Web-inherent ports implemented by Api itself (ADR-0024). IOrderNotifier/OrderTrackingHub
// (SignalR) are Iteration 4 (api-layer.md 10) — not registered yet, nothing in Iteration 1
// resolves them.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// 4. Controllers, ProblemDetails, Swagger with a Bearer security definition.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PizzaShop API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

// 5. JWT bearer authentication (api-layer.md 2.7/9). MapInboundClaims = false keeps the raw
// claim types JwtTokenGenerator wrote ("sub"/"customerId") instead of ASP.NET Core's default
// inbound remapping — HttpContextCurrentUser relies on that.
//
// TokenValidationParameters are bound lazily through the options pattern (PostConfigure via
// AddOptions<JwtBearerOptions>().Configure<IOptions<JwtOptions>>) rather than read eagerly from
// builder.Configuration here. This matters for WebApplicationFactory-based tests
// (tests/PizzaShop.Api.Tests): ConfigureWebHost's configuration overrides are only guaranteed
// to be merged into the final configuration by the time the host finishes building — reading
// builder.Configuration synchronously at this point (before builder.Build()) would capture the
// pre-override snapshot, causing token validation to use a different signing key than
// JwtTokenGenerator (which resolves the same IOptions<JwtOptions> lazily, after the host is
// built) actually signs with.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        bearerOptions.MapInboundClaims = false;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

// 6. Authorization — FallbackPolicy requires authentication so a forgotten [Authorize]
// attribute never leaves an endpoint open; public endpoints opt out with [AllowAnonymous]
// (api-layer.md 5).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 7. SignalR (OrderTrackingHub) and 8. CORS are Iteration 4/future work (api-layer.md 8-9) —
// nothing in Iteration 1 needs them, so they are deliberately left out for now.

// 9. Global exception -> ProblemDetails mapping (api-layer.md 4, ADR-0027).
builder.Services.AddExceptionHandler<ExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Skipped entirely in the "Testing" environment (WebApplicationFactory-based Api tests,
// tests/PizzaShop.Api.Tests) — those tests replace the identity repositories with in-memory
// fakes and never touch a real Postgres instance (no Docker/Testcontainers requirement for
// this project, ADR-0025 covers that for PizzaShop.Infrastructure.Tests instead).
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PizzaShopDbContext>();
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(app.Services);
}

app.Run();

/// <summary>Exposes the entry point to <c>WebApplicationFactory&lt;Program&gt;</c> in tests/PizzaShop.Api.Tests.</summary>
public partial class Program
{
}
