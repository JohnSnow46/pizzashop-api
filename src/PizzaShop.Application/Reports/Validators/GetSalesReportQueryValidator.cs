using FluentValidation;
using PizzaShop.Application.Reports.Queries;

namespace PizzaShop.Application.Reports.Validators;

/// <summary>Shape-only validation (ADR-0012) — the report is read-only, so there is no domain state to guard.</summary>
public sealed class GetSalesReportQueryValidator : AbstractValidator<GetSalesReportQuery>
{
    public GetSalesReportQueryValidator()
    {
        RuleFor(q => q.From)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("'From' is required.");

        RuleFor(q => q.To)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("'To' is required.");

        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From)
            .WithMessage("'To' must be on or after 'From'.");

        RuleFor(q => q.TopItems).InclusiveBetween(1, 50);
    }
}
