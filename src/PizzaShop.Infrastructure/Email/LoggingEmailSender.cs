using Microsoft.Extensions.Logging;
using PizzaShop.Application.Abstractions.Email;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Infrastructure.Email;

/// <summary>Placeholder <see cref="IEmailSender"/> for local/dev use — logs instead of sending
/// real SMTP mail. Swap for a real provider behind the same port when one is needed.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", email, token);
        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationEmailAsync(
        string email, string orderNumber, Guid orderId, Guid? guestTrackingToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order confirmation for {Email}. Order: {OrderNumber} ({OrderId})", email, orderNumber, orderId);
        LogTrackingHint(email, guestTrackingToken);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusChangedEmailAsync(
        string email, string orderNumber, Guid orderId, OrderStatus newStatus, Guid? guestTrackingToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order status changed for {Email}. Order: {OrderNumber} ({OrderId}). New status: {NewStatus}",
            email, orderNumber, orderId, newStatus);
        LogTrackingHint(email, guestTrackingToken);
        return Task.CompletedTask;
    }

    private void LogTrackingHint(string email, Guid? guestTrackingToken)
    {
        if (guestTrackingToken is { } token)
            _logger.LogInformation("Tracking link for {Email}: /track/{GuestTrackingToken}", email, token);
        else
            _logger.LogInformation("{Email} can track this order from their customer account.", email);
    }
}
