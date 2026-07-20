namespace PizzaShop.Infrastructure.Payments.PayU;

/// <summary>
/// Configuration for <see cref="PayUPaymentGateway"/> (ADR-0022). Sandbox vs. production is
/// purely a matter of which values are configured here (<see cref="BaseUrl"/>, sandbox POS
/// credentials) — no code changes (ADR-0002).
/// </summary>
public sealed class PayUOptions
{
    /// <summary>Sandbox by default: <c>https://secure.snd.payu.com</c>.</summary>
    public string BaseUrl { get; set; } = "https://secure.snd.payu.com";

    public string PosId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>PayU's "second key" (MD5) used to verify the <c>OpenPayU-Signature</c> header.</summary>
    public string SecondKey { get; set; } = string.Empty;

    /// <summary>Webhook URL in Api that PayU calls with payment notifications.</summary>
    public string NotifyUrl { get; set; } = string.Empty;

    /// <summary>URL the customer's browser returns to after paying.</summary>
    public string ContinueUrl { get; set; } = string.Empty;
}
