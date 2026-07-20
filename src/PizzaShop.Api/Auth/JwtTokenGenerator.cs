using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Api.Auth;

/// <summary>
/// <see cref="IJwtTokenGenerator"/> implementation (api-layer.md 2.7, ADR-0024/0026). Lives in
/// Api, not Infrastructure, because it needs the signing configuration and is symmetric to
/// <see cref="HttpContextCurrentUser"/>, which reads these same claims back. Claims:
/// <c>sub</c> = <see cref="UserAccount.Id"/>, <see cref="ClaimTypes.Role"/> = account role,
/// <c>email</c>, and (only for a Customer account) <c>customerId</c>. Program.cs sets
/// <c>MapInboundClaims = false</c> on the bearer handler so these raw claim types survive
/// unchanged when the token is parsed back.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Generate(UserAccount account, Guid? customerId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
        };

        if (customerId is { } id)
            claims.Add(new Claim(HttpContextCurrentUser.CustomerIdClaimType, id.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
