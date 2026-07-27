using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Commands;

/// <summary>
/// Uploads and stores a new image for an existing <c>MenuItem</c> (via
/// <see cref="Abstractions.Storage.IMenuItemImageStorage"/>), replacing
/// <c>MenuItem.ImageUrl</c>. Returns the new relative image URL.
/// </summary>
public sealed record UploadMenuItemImageCommand(
    Guid MenuItemId,
    byte[] Content,
    string ContentType,
    string FileExtension) : ICommand<string>;
