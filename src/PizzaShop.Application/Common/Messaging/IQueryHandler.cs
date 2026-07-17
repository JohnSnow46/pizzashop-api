namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Handles a single <typeparamref name="TQuery"/> and returns <typeparamref name="TResponse"/>.
/// One handler per file (CLAUDE.md).
/// </summary>
public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
