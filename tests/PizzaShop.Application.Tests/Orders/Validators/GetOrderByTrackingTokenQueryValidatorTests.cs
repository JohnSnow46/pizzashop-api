using FluentAssertions;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class GetOrderByTrackingTokenQueryValidatorTests
{
    private readonly GetOrderByTrackingTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyToken_HasNoErrors()
    {
        var result = _validator.Validate(new GetOrderByTrackingTokenQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyToken_HasErrorForGuestTrackingToken()
    {
        var result = _validator.Validate(new GetOrderByTrackingTokenQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrderByTrackingTokenQuery.GuestTrackingToken));
    }
}
