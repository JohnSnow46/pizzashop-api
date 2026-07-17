using FluentAssertions;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Dtos;
using PizzaShop.Application.Restaurant.Validators;

namespace PizzaShop.Application.Tests.Restaurant.Validators;

public class UpdateOpeningHoursCommandValidatorTests
{
    private readonly UpdateOpeningHoursCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>
        {
            [DayOfWeek.Monday] = new[] { new TimeRangeDto(new TimeOnly(10, 0), new TimeOnly(22, 0)) },
        };
        var command = new UpdateOpeningHoursCommand(new OpeningHoursDto(schedule));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullOpeningHours_HasErrorForOpeningHours()
    {
        var command = new UpdateOpeningHoursCommand(null!);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOpeningHoursCommand.OpeningHours));
    }

    [Fact]
    public void Validate_RangeEndBeforeStart_HasError()
    {
        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>>
        {
            [DayOfWeek.Monday] = new[] { new TimeRangeDto(new TimeOnly(22, 0), new TimeOnly(10, 0)) },
        };
        var command = new UpdateOpeningHoursCommand(new OpeningHoursDto(schedule));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
