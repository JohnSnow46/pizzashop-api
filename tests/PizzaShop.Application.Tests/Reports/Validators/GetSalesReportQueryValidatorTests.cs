using FluentAssertions;
using PizzaShop.Application.Reports.Queries;
using PizzaShop.Application.Reports.Validators;

namespace PizzaShop.Application.Tests.Reports.Validators;

public class GetSalesReportQueryValidatorTests
{
    private readonly GetSalesReportQueryValidator _validator = new();

    private static readonly DateTimeOffset From = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 7, 31, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_ValidQuery_HasNoErrors()
    {
        var result = _validator.Validate(new GetSalesReportQuery(From, To, 5));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ToBeforeFrom_HasErrorForTo()
    {
        var result = _validator.Validate(new GetSalesReportQuery(To, From, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetSalesReportQuery.To));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_TopItemsOutOfRange_HasErrorForTopItems(int topItems)
    {
        var result = _validator.Validate(new GetSalesReportQuery(From, To, topItems));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetSalesReportQuery.TopItems));
    }

    [Fact]
    public void Validate_DefaultFrom_ReturnsError()
    {
        var result = _validator.Validate(new GetSalesReportQuery(default, To, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetSalesReportQuery.From));
    }

    [Fact]
    public void Validate_DefaultTo_ReturnsError()
    {
        var result = _validator.Validate(new GetSalesReportQuery(From, default, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetSalesReportQuery.To));
    }
}
