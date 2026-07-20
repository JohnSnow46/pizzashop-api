using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Infrastructure.Time;

/// <summary>
/// <see cref="IClock"/> backed by the system clock — always UTC (offset zero), required for
/// Npgsql's <c>timestamptz</c> mapping (ADR-0010).
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
