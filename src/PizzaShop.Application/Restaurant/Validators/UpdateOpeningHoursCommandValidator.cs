using FluentValidation;
using PizzaShop.Application.Restaurant.Commands;

namespace PizzaShop.Application.Restaurant.Validators;

public sealed class UpdateOpeningHoursCommandValidator : AbstractValidator<UpdateOpeningHoursCommand>
{
    public UpdateOpeningHoursCommandValidator()
    {
        RuleFor(c => c.OpeningHours).NotNull();

        When(c => c.OpeningHours is not null, () =>
        {
            RuleForEach(c => c.OpeningHours.Schedule.Values)
                .Must(ranges => ranges.All(r => r.End > r.Start))
                .WithMessage("Each opening hours range must have an end time after its start time.");
        });
    }
}
