using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Api.Auth;

/// <summary>
/// <see cref="ICurrentUser"/> implementation reading claims off the current
/// <see cref="HttpContext"/> (api-layer.md 3, ADR-0024/0026/0027). Program.cs sets
/// <c>MapInboundClaims = false</c> on the JWT bearer handler, so the claim types here match
/// exactly what <see cref="JwtTokenGenerator"/> wrote (raw <c>sub</c>/<c>customerId</c>, the
/// full <see cref="ClaimTypes.Role"/> URI) — no ASP.NET Core inbound remapping to account for.
/// No <see cref="HttpContext"/> or an unauthenticated request leaves every member <c>null</c>
/// (guest, ADR-0005).
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    public const string CustomerIdClaimType = "customerId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserAccountId => ParseGuid(JwtRegisteredClaimNames.Sub);

    public Guid? CustomerId => ParseGuid(CustomerIdClaimType);

    public UserRole? Role
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return value is not null && Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    private Guid? ParseGuid(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        return value is not null && Guid.TryParse(value, out var id) ? id : null;
    }
}
