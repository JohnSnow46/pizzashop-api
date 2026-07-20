using FluentAssertions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Validators;

namespace PizzaShop.Application.Tests.Identity.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new LoginCommand("jan@example.com", "whatever-password"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrorForEmail()
    {
        var result = _validator.Validate(new LoginCommand("not-an-email", "whatever-password"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_EmptyPassword_HasErrorForPassword()
    {
        var result = _validator.Validate(new LoginCommand("jan@example.com", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_ShortLegacyPassword_HasNoErrors()
    {
        // Login intentionally has no MinimumLength — must keep accepting passwords set before
        // any minimum-length policy existed.
        var result = _validator.Validate(new LoginCommand("jan@example.com", "short"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordOver100Characters_HasErrorForPassword()
    {
        var result = _validator.Validate(new LoginCommand("jan@example.com", new string('a', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
