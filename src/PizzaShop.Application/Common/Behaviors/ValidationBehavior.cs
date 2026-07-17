using FluentValidation;
using ValidationException = PizzaShop.Application.Common.Exceptions.ValidationException;

namespace PizzaShop.Application.Common.Behaviors;

/// <summary>
/// Runs all registered <see cref="IValidator{T}"/> for a request before it reaches its
/// handler (ADR-0012). Validators check only the shape of the data (required fields,
/// formats, ranges) — state-dependent business rules stay in Domain (CLAUDE.md).
/// </summary>
public sealed class ValidationBehavior<TRequest>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return;

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }
}
