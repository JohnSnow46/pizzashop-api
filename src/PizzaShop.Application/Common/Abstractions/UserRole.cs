namespace PizzaShop.Application.Common.Abstractions;

/// <summary>
/// Roles a request can act under (ADR-0004). Identity/role lives outside Domain
/// (ADR-0005) — this is the Application-level representation of the account role, as
/// supplied by Api from the JWT claim.
/// </summary>
public enum UserRole
{
    Customer,
    Employee,
    RestaurantAdmin,
    SuperAdmin,
}
