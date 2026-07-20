namespace PizzaShop.Api.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section (api-layer.md 2.7, ADR-0026). The signing
/// key MUST come from user-secrets/environment in every real environment — the placeholder in
/// <c>appsettings.json</c> is not a usable secret (see the comment there).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
