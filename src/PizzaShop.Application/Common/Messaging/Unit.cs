namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Represents "no meaningful value" — the response type for commands that only change
/// state and have nothing useful to return (ADR-0012).
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
