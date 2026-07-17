using FluentAssertions;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class GetOrderByIdQueryValidatorTests
{
    private readonly GetOrderByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_HasNoErrors()
    {
        var result = _validator.Validate(new GetOrderByIdQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_HasErrorForOrderId()
    {
        var result = _validator.Validate(new GetOrderByIdQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrderByIdQuery.OrderId));
    }
}
