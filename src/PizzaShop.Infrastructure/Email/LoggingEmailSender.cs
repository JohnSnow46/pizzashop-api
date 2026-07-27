using Microsoft.Extensions.Logging;
using PizzaShop.Application.Abstractions.Email;

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
}
