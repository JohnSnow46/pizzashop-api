using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using PizzaShop.Application.Common.Behaviors;
using ValidationException = PizzaShop.Application.Common.Exceptions.ValidationException;

namespace PizzaShop.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Value);

    [Fact]
    public async Task ValidateAsync_NoValidators_DoesNotThrow()
    {
        var behavior = new ValidationBehavior<SampleRequest>(Array.Empty<IValidator<SampleRequest>>());

        var act = () => behavior.ValidateAsync(new SampleRequest("x"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_ValidatorReportsFailure_ThrowsValidationExceptionWithErrors()
    {
        var failingValidator = new Mock<IValidator<SampleRequest>>();
        failingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Value is required.") }));

        var behavior = new ValidationBehavior<SampleRequest>(new[] { failingValidator.Object });

        var act = () => behavior.ValidateAsync(new SampleRequest(""), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task ValidateAsync_AllValidatorsPass_DoesNotThrow()
    {
        var passingValidator = new Mock<IValidator<SampleRequest>>();
        passingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<SampleRequest>(new[] { passingValidator.Object });

        var act = () => behavior.ValidateAsync(new SampleRequest("ok"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
