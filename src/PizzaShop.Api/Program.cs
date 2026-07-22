using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PizzaShop.Api;
using PizzaShop.Api.Auth;
using PizzaShop.Api.Middleware;
using PizzaShop.Api.Realtime;
using PizzaShop.Application;
using PizzaShop.Application.Abstractions.Realtime;
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

// 3. Web-inherent ports implemented by Api itself (ADR-0024). IOrderNotifier is backed by
// OrderTrackingHub (SignalR, api-layer.md 8, ADR-0028) via SignalROrderNotifier — the
// Iteration 3 temporary NoopOrderNotifier (ADR-0031) has been removed now that the hub exists.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IOrderNotifier, SignalROrderNotifier>();
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

        // SignalR WebSocket/SSE clients can't set an Authorization header, so
        // OrderTrackingHub's logged-in path (SubscribeToOrder, api-layer.md 8.1/9, ADR-0028)
        // authenticates via an "access_token" query string parameter instead. Scoped to the
        // hub's own path only — every other endpoint still requires the Authorization header.
        bearerOptions.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/order-tracking"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
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

// 7. SignalR (OrderTrackingHub, api-layer.md 8, ADR-0028). HubHttpContextFilter fixes a
// real IHttpContextAccessor/ICurrentUser gap inside hub methods — see its doc comment.
builder.Services.AddSignalR(options => options.AddFilter<HubHttpContextFilter>());

// 8. CORS is future work (api-layer.md 9) — no frontend origin to configure yet, so it is
// deliberately left out for now.

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
app.MapHub<OrderTrackingHub>("/hubs/order-tracking");

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
