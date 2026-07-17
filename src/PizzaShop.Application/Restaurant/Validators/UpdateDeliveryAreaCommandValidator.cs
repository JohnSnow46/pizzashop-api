using FluentValidation;
using PizzaShop.Application.Restaurant.Commands;

namespace PizzaShop.Application.Restaurant.Validators;

public sealed class UpdateDeliveryAreaCommandValidator : AbstractValidator<UpdateDeliveryAreaCommand>
{
    public UpdateDeliveryAreaCommandValidator()
    {
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);
        RuleFor(c => c.DeliveryRadiusKm).GreaterThan(0);
    }
}
