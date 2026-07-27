using FluentValidation;
using PizzaShop.Application.Catalog.Commands;

namespace PizzaShop.Application.Catalog.Validators;

/// <summary>
/// Shape-only validation (ADR-0012) for <see cref="UploadMenuItemImageCommand"/>: size and
/// declared content type/extension. Whether the item itself exists is a Domain/handler
/// concern (<see cref="Common.Exceptions.NotFoundException"/>), not this validator's job.
/// </summary>
public sealed class UploadMenuItemImageCommandValidator : AbstractValidator<UploadMenuItemImageCommand>
{
    private const int MaxContentBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public UploadMenuItemImageCommandValidator()
    {
        RuleFor(c => c.MenuItemId).NotEmpty();

        RuleFor(c => c.Content)
            .NotEmpty()
            .Must(content => content.Length <= MaxContentBytes)
            .WithMessage($"Image must be at most {MaxContentBytes} bytes.");

        RuleFor(c => c.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage($"ContentType must be one of: {string.Join(", ", AllowedContentTypes)}.");

        RuleFor(c => c.FileExtension)
            .Must(extension => AllowedExtensions.Contains(extension.ToLowerInvariant()))
            .WithMessage($"FileExtension must be one of: {string.Join(", ", AllowedExtensions)}.");
    }
}
