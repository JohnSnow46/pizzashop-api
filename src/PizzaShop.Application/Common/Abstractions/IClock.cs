namespace PizzaShop.Application.Common.Abstractions;

/// <summary>
/// Testable source of the current time (ADR-0010). Always UTC.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
