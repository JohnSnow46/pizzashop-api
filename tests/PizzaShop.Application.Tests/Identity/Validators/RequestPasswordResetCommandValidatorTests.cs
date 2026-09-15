using FluentAssertions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Validators;

namespace PizzaShop.Application.Tests.Identity.Validators;

public class RequestPasswordResetCommandValidatorTests
{
    private readonly RequestPasswordResetCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new RequestPasswordResetCommand("jan@example.com"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrorForEmail()
    {
        var result = _validator.Validate(new RequestPasswordResetCommand("not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestPasswordResetCommand.Email));
    }

    [Fact]
    public void Validate_EmailOver200Characters_HasErrorForEmail()
    {
        var email = new string('a', 190) + "@example.com";
        var result = _validator.Validate(new RequestPasswordResetCommand(email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestPasswordResetCommand.Email));
    }
}
