using FluentValidation.Results;

namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown by <see cref="Behaviors.ValidationBehavior{TRequest}"/> when one or more
/// FluentValidation validators fail. Application-level, distinct from Domain's
/// <c>DomainException</c> — mapped to HTTP 400 in the Api middleware (application-layer.md 5).
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyCollection<ValidationError> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();
    }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors.ToList();
    }
}

public sealed record ValidationError(string PropertyName, string ErrorMessage);
