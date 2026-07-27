using PizzaShop.Application.Abstractions.Storage;

namespace PizzaShop.Api.Storage;

/// <summary>
/// Stores MenuItem images on local disk under <c>wwwroot/uploads/menu-items</c>, served back
/// by <c>app.UseStaticFiles()</c> (Program.cs). Lives in Api, not Infrastructure — local disk
/// storage under <c>wwwroot</c> is a web-hosting concept (ADR-0024).
/// </summary>
public sealed class LocalMenuItemImageStorage : IMenuItemImageStorage
{
    private const string RelativeUploadsRoot = "/uploads/menu-items";

    private readonly IWebHostEnvironment _environment;

    public LocalMenuItemImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveAsync(Guid menuItemId, byte[] content, string fileExtension, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_environment.WebRootPath, "uploads", "menu-items");
        Directory.CreateDirectory(directory);

        foreach (var existingFile in Directory.GetFiles(directory, $"{menuItemId}.*"))
            File.Delete(existingFile);

        var fileName = $"{menuItemId}{fileExtension}";
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        return $"{RelativeUploadsRoot}/{fileName}";
    }
}
