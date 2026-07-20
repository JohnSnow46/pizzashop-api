namespace PizzaShop.Api.Auth;

/// <summary>
/// Explicit role lists for <c>[Authorize(Roles=...)]</c> (api-layer.md 5, ADR-0027, CLAUDE.md).
/// The role hierarchy (<c>SuperAdmin</c> &#8839; <c>RestaurantAdmin</c> &#8839; <c>Employee</c>)
/// is enforced by spelling out every allowed role per constant — never by the token or Domain.
/// </summary>
public static class AuthRoles
{
    /// <summary>Employee and above.</summary>
    public const string Staff = "Employee,RestaurantAdmin,SuperAdmin";

    /// <summary>RestaurantAdmin and above.</summary>
    public const string Admin = "RestaurantAdmin,SuperAdmin";

    /// <summary>SuperAdmin only.</summary>
    public const string Owner = "SuperAdmin";

    /// <summary>Customer only (own data).</summary>
    public const string Customer = "Customer";
}
