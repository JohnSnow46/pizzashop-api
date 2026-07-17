using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PizzaShop.Application.Common.Behaviors;

namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Default <see cref="IDispatcher"/> implementation: resolves the handler and any
/// registered validators from DI, runs <see cref="ValidationBehavior{TRequest}"/>, then
/// invokes the handler (ADR-0012).
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private static readonly MethodInfo SendCommandCoreDefinition =
        typeof(Dispatcher).GetMethod(nameof(SendCommandCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo SendQueryCoreDefinition =
        typeof(Dispatcher).GetMethod(nameof(SendQueryCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    // Constructing a closed generic method via MakeGenericMethod is comparatively expensive;
    // cache the closed MethodInfo per (message type, response type) so repeat Send calls for
    // the same command/query type skip that reflection cost.
    private static readonly ConcurrentDictionary<(Type MessageType, Type ResponseType), MethodInfo> CommandCoreCache = new();
    private static readonly ConcurrentDictionary<(Type MessageType, Type ResponseType), MethodInfo> QueryCoreCache = new();

    private readonly IServiceProvider _services;

    public Dispatcher(IServiceProvider services)
    {
        _services = services;
    }

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var method = CommandCoreCache.GetOrAdd(
            (command.GetType(), typeof(TResponse)),
            key => SendCommandCoreDefinition.MakeGenericMethod(key.MessageType, key.ResponseType));
        return (Task<TResponse>)method.Invoke(this, new object[] { command, cancellationToken })!;
    }

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var method = QueryCoreCache.GetOrAdd(
            (query.GetType(), typeof(TResponse)),
            key => SendQueryCoreDefinition.MakeGenericMethod(key.MessageType, key.ResponseType));
        return (Task<TResponse>)method.Invoke(this, new object[] { query, cancellationToken })!;
    }

    private async Task<TResponse> SendCommandCore<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var validators = _services.GetServices<IValidator<TCommand>>();
        await new ValidationBehavior<TCommand>(validators).ValidateAsync(command, cancellationToken);

        var handler = _services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, cancellationToken);
    }

    private async Task<TResponse> SendQueryCore<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        var validators = _services.GetServices<IValidator<TQuery>>();
        await new ValidationBehavior<TQuery>(validators).ValidateAsync(query, cancellationToken);

        var handler = _services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.Handle(query, cancellationToken);
    }
}
