namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Thin dispatcher resolving handlers from DI and running the validation pipeline before
/// invoking them (ADR-0012 — deliberately not MediatR). Handlers remain directly
/// unit-testable without going through the dispatcher.
/// </summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
