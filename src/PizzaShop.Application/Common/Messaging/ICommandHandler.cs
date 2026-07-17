namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Handles a single <typeparamref name="TCommand"/> and returns <typeparamref name="TResponse"/>.
/// One handler per file (CLAUDE.md).
/// </summary>
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a single <typeparamref name="TCommand"/> that has no meaningful value to return.
/// </summary>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit> where TCommand : ICommand
{
}
