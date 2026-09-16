using FluentValidation;
using PizzaShop.Application.Orders.Queries;

namespace PizzaShop.Application.Orders.Validators;

public sealed class CheckDeliveryAvailabilityQueryValidator : AbstractValidator<CheckDeliveryAvailabilityQuery>
{
    public CheckDeliveryAvailabilityQueryValidator()
    {
        RuleFor(q => q.Address).NotNull();

        When(q => q.Address is not null, () =>
        {
            RuleFor(q => q.Address.Street).NotEmpty().MaximumLength(200);
            RuleFor(q => q.Address.BuildingNumber).NotEmpty().MaximumLength(20);
            RuleFor(q => q.Address.City).NotEmpty().MaximumLength(100);
            RuleFor(q => q.Address.PostalCode).NotEmpty().MaximumLength(10);
        });
    }
}
