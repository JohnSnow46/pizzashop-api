namespace PizzaShop.Application.Abstractions.Storage;

/// <summary>
/// Persists a <c>MenuItem</c>'s uploaded image and returns the relative URL to store in
/// <c>MenuItem.ImageUrl</c>. Implemented by <c>LocalMenuItemImageStorage</c> in Api, not
/// Infrastructure — local disk storage under <c>wwwroot</c> is a web-hosting concept
/// (ADR-0024), mirroring <see cref="Realtime.IOrderNotifier"/>/<c>ICurrentUser</c>.
/// </summary>
public interface IMenuItemImageStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> as the image for <paramref name="menuItemId"/>,
    /// replacing any previously stored image regardless of its extension, and returns the
    /// relative URL (e.g. <c>/uploads/menu-items/{menuItemId}{fileExtension}</c>).
    /// </summary>
    Task<string> SaveAsync(Guid menuItemId, byte[] content, string fileExtension, CancellationToken cancellationToken);
}
