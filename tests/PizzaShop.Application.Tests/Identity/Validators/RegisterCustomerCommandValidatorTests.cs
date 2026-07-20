using FluentAssertions;
using PizzaShop.Application.Identity.Commands;
using PizzaShop.Application.Identity.Validators;

namespace PizzaShop.Application.Tests.Identity.Validators;

public class RegisterCustomerCommandValidatorTests
{
    private readonly RegisterCustomerCommandValidator _validator = new();

    private static RegisterCustomerCommand ValidCommand() =>
        new("jan@example.com", "Password123", "Jan Kowalski", "123456789");

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrorForEmail()
    {
        var command = ValidCommand() with { Email = "not-an-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerCommand.Email));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("alllettersnodigits")]
    [InlineData("12345678")]
    public void Validate_WeakPassword_HasErrorForPassword(string password)
    {
        var command = ValidCommand() with { Password = password };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerCommand.Password));
    }

    [Fact]
    public void Validate_EmptyFullName_HasErrorForFullName()
    {
        var command = ValidCommand() with { FullName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerCommand.FullName));
    }

    [Fact]
    public void Validate_PasswordOver100Characters_HasErrorForPassword()
    {
        var command = ValidCommand() with { Password = "Aa1" + new string('a', 100) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerCommand.Password));
    }
}
