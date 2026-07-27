namespace PizzaShop.Application.Abstractions.Email;

/// <summary>
/// Outbound email port, mirroring the shape of <see cref="Geocoding.IGeocodingService"/> —
/// a general Application port, not identity-specific. Implemented by
/// <c>LoggingEmailSender</c> in Infrastructure until a real provider is needed.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken);
}
