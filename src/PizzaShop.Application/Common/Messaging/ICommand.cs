namespace PizzaShop.Application.Common.Messaging;

/// <summary>
/// Marker for a command that changes state and returns <typeparamref name="TResponse"/>
/// (ADR-0012). Handled by exactly one <see cref="ICommandHandler{TCommand,TResponse}"/>.
/// </summary>
public interface ICommand<TResponse>
{
}

/// <summary>
/// Marker for a command that changes state but has no meaningful value to return.
/// </summary>
public interface ICommand : ICommand<Unit>
{
}
