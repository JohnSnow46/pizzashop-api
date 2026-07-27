using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Validators;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class UploadMenuItemImageCommandValidatorTests
{
    private readonly UploadMenuItemImageCommandValidator _validator = new();

    private static UploadMenuItemImageCommand ValidCommand() =>
        new(Guid.NewGuid(), new byte[] { 1, 2, 3 }, "image/png", ".png");

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMenuItemId_HasErrorForMenuItemId()
    {
        var result = _validator.Validate(ValidCommand() with { MenuItemId = Guid.Empty });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadMenuItemImageCommand.MenuItemId));
    }

    [Fact]
    public void Validate_EmptyContent_HasErrorForContent()
    {
        var result = _validator.Validate(ValidCommand() with { Content = Array.Empty<byte>() });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadMenuItemImageCommand.Content));
    }

    [Fact]
    public void Validate_ContentOverFiveMegabytes_HasErrorForContent()
    {
        var oversized = new byte[5 * 1024 * 1024 + 1];

        var result = _validator.Validate(ValidCommand() with { Content = oversized });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadMenuItemImageCommand.Content));
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    public void Validate_UnsupportedContentType_HasErrorForContentType(string contentType)
    {
        var result = _validator.Validate(ValidCommand() with { ContentType = contentType });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadMenuItemImageCommand.ContentType));
    }

    [Theory]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    public void Validate_UnsupportedFileExtension_HasErrorForFileExtension(string extension)
    {
        var result = _validator.Validate(ValidCommand() with { FileExtension = extension });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadMenuItemImageCommand.FileExtension));
    }
}
