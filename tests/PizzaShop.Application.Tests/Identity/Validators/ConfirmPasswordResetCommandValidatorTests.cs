using FluentAssertions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Validators;

namespace PizzaShop.Application.Tests.Identity.Validators;

public class ConfirmPasswordResetCommandValidatorTests
{
    private readonly ConfirmPasswordResetCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new ConfirmPasswordResetCommand("token-123", "Password1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyToken_HasErrorForToken()
    {
        var result = _validator.Validate(new ConfirmPasswordResetCommand("", "Password1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmPasswordResetCommand.Token));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("nouppercasenodigit")]
    [InlineData("NoDigitsHere")]
    [InlineData("12345678")]
    public void Validate_WeakPassword_HasErrorForNewPassword(string password)
    {
        var result = _validator.Validate(new ConfirmPasswordResetCommand("token-123", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmPasswordResetCommand.NewPassword));
    }

    [Fact]
    public void Validate_PasswordOver100Characters_HasErrorForNewPassword()
    {
        var password = "Aa1" + new string('a', 98);
        var result = _validator.Validate(new ConfirmPasswordResetCommand("token-123", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmPasswordResetCommand.NewPassword));
    }
}
