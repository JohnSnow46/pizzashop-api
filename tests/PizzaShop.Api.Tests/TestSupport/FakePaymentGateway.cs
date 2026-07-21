using System.Text.Json;
using System.Text.Json.Serialization;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// Deterministic <see cref="IPaymentGateway"/> for tests — the real implementation
/// (<c>PayUPaymentGateway</c>, Infrastructure) calls the PayU Sandbox over HTTP, which this
/// project deliberately never does (see <see cref="InMemoryUserAccountRepository"/> for the
/// same rationale applied to persistence).
/// <see cref="VerifyAndParseNotification"/> stands in for real HMAC signature verification
/// (ADR-0013/0022): it accepts only the sentinel <see cref="ValidSignature"/> value on the
/// <c>OpenPayU-Signature</c> header, and parses <paramref name="rawBody"/>-equivalent JSON
/// shaped like <see cref="WebhookPayload"/> — tests build that payload with
/// <see cref="BuildNotificationBody"/> instead of PayU's real notification schema, since the
/// real parsing/mapping is Infrastructure's concern, not Api's.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public const string SignatureHeaderName = "OpenPayU-Signature";
    public const string ValidSignature = "test-signature";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<PaymentInitResult> InitializePaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentInitResult(
            $"https://payu.test/checkout/{request.OrderId:N}",
            $"payu-ref-{request.OrderId:N}"));

    public PaymentNotification VerifyAndParseNotification(string rawBody, IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue(SignatureHeaderName, out var signature) || signature != ValidSignature)
            throw new InvalidPaymentNotificationException($"Missing or invalid '{SignatureHeaderName}' header.");

        var payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody, JsonOptions)
            ?? throw new InvalidPaymentNotificationException("Malformed notification payload.");

        return new PaymentNotification(payload.OrderId, payload.ProviderPaymentReference, payload.Status);
    }

    public Task RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    public static string BuildNotificationBody(Guid orderId, string providerPaymentReference, PaymentStatus status) =>
        JsonSerializer.Serialize(new WebhookPayload(orderId, providerPaymentReference, status), JsonOptions);

    private sealed record WebhookPayload(Guid OrderId, string ProviderPaymentReference, PaymentStatus Status);
}
