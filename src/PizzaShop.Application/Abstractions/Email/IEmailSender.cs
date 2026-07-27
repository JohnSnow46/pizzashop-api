using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Abstractions.Email;

/// <summary>
/// Outbound email port, mirroring the shape of <see cref="Geocoding.IGeocodingService"/> —
/// a general Application port, not identity-specific. Implemented by
/// <c>LoggingEmailSender</c> in Infrastructure until a real provider is needed.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken);

    /// <summary>
    /// Order-placed confirmation (<c>CreateOrderCommandHandler</c>). <paramref name="guestTrackingToken"/>
    /// is the guest's tracking-link token (non-null for a guest order, <c>null</c> for a
    /// registered customer, who tracks the order through their own account instead).
    /// </summary>
    Task SendOrderConfirmationEmailAsync(
        string email, string orderNumber, Guid orderId, Guid? guestTrackingToken, CancellationToken cancellationToken);

    /// <summary>
    /// Order fulfillment status change (Accept/Reject/MarkReady/StartDelivery/Complete/Cancel).
    /// <paramref name="guestTrackingToken"/> is non-null only for a guest order.
    /// </summary>
    Task SendOrderStatusChangedEmailAsync(
        string email, string orderNumber, Guid orderId, OrderStatus newStatus, Guid? guestTrackingToken, CancellationToken cancellationToken);
}
