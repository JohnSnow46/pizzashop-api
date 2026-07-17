using FluentValidation;
using PizzaShop.Application.Orders.Queries;

namespace PizzaShop.Application.Orders.Validators;

public sealed class GetOrderByTrackingTokenQueryValidator : AbstractValidator<GetOrderByTrackingTokenQuery>
{
    public GetOrderByTrackingTokenQueryValidator()
    {
        RuleFor(q => q.GuestTrackingToken).NotEmpty();
    }
}
