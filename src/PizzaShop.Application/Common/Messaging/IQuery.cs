namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Marker for a query that reads data and returns <typeparamref name="TResponse"/> (a DTO,
/// never a Domain entity) without changing state (ADR-0012).
/// </summary>
public interface IQuery<TResponse>
{
}
