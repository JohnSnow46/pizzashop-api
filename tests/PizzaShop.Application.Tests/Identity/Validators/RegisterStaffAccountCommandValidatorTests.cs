using FluentAssertions;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Validators;

namespace PizzaShop.Application.Tests.Identity.Validators;

public class RegisterStaffAccountCommandValidatorTests
{
    private readonly RegisterStaffAccountCommandValidator _validator = new();

    private static RegisterStaffAccountCommand ValidCommand() =>
        new("staff@example.com", "Password123", UserRole.Employee);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CustomerRole_HasErrorForRole()
    {
        var command = ValidCommand() with { Role = UserRole.Customer };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStaffAccountCommand.Role));
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrorForEmail()
    {
        var command = ValidCommand() with { Email = "not-an-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStaffAccountCommand.Email));
    }

    [Fact]
    public void Validate_WeakPassword_HasErrorForPassword()
    {
        var command = ValidCommand() with { Password = "short" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStaffAccountCommand.Password));
    }

    [Fact]
    public void Validate_PasswordOver100Characters_HasErrorForPassword()
    {
        var command = ValidCommand() with { Password = "Aa1" + new string('a', 100) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStaffAccountCommand.Password));
    }
}
