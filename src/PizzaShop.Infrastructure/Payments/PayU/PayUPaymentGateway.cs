using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Common.Exceptions;

namespace PizzaShop.Infrastructure.Payments.PayU;

/// <summary>
/// <see cref="IPaymentGateway"/> implementation against PayU's REST API (ADR-0002/ADR-0013/
/// ADR-0022). Sandbox vs. production is a matter of <see cref="PayUOptions"/> values only.
/// </summary>
public sealed class PayUPaymentGateway : IPaymentGateway
{
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _cachedAccessToken;
    private static DateTimeOffset _accessTokenExpiresAt;

    private readonly HttpClient _httpClient;
    private readonly PayUOptions _options;

    public PayUPaymentGateway(HttpClient httpClient, IOptions<PayUOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
    }

    public async Task<PaymentInitResult> InitializePaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var amountInGrosze = ToGrosze(request.Amount.Amount);

        var payUOrder = new PayUCreateOrderRequest(
            NotifyUrl: _options.NotifyUrl,
            ContinueUrl: _options.ContinueUrl,
            CustomerIp: "127.0.0.1",
            MerchantPosId: _options.PosId,
            Description: request.Description,
            CurrencyCode: request.Amount.Currency,
            TotalAmount: amountInGrosze,
            ExtOrderId: request.OrderId.ToString(),
            Buyer: request.CustomerEmail is null ? null : new PayUBuyer(request.CustomerEmail),
            Products: new[] { new PayUProduct(request.Description, amountInGrosze, "1") });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2_1/orders")
        {
            Content = JsonContent.Create(payUOrder),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // PayU signals success for the checkout flow via a 302 redirect — the typed
        // HttpClient must not follow it automatically (see DependencyInjection).
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        PayUCreateOrderResponse? body = null;
        if (response.Content.Headers.ContentLength is > 0)
            body = await response.Content.ReadFromJsonAsync<PayUCreateOrderResponse>(cancellationToken: cancellationToken);

        var redirectUrl = response.Headers.Location?.ToString() ?? body?.RedirectUri
            ?? throw new InvalidOperationException("PayU did not return a redirect URL for the payment session.");

        var providerPaymentReference = body?.OrderId
            ?? throw new InvalidOperationException("PayU did not return an order id for the payment session.");

        return new PaymentInitResult(redirectUrl, providerPaymentReference);
    }

    public PaymentNotification VerifyAndParseNotification(string rawBody, IReadOnlyDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue("OpenPayU-Signature", out var signatureHeader))
            throw new InvalidPaymentNotificationException("Missing 'OpenPayU-Signature' header.");

        if (!PayUSignatureVerifier.Verify(rawBody, signatureHeader, _options.SecondKey))
            throw new InvalidPaymentNotificationException("PayU notification signature is missing or invalid.");

        PayUNotificationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PayUNotificationPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            throw new InvalidPaymentNotificationException($"Malformed PayU notification payload: {ex.Message}");
        }

        var order = payload?.Order
            ?? throw new InvalidPaymentNotificationException("PayU notification payload is missing the 'order' section.");

        if (string.IsNullOrWhiteSpace(order.ExtOrderId) || !Guid.TryParse(order.ExtOrderId, out var orderId))
            throw new InvalidPaymentNotificationException("PayU notification payload has a missing/invalid 'extOrderId'.");

        var status = PayUStatusMapper.Map(order.Status);

        return new PaymentNotification(orderId, order.OrderId, status);
    }

    public async Task RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v2_1/orders/{request.ProviderPaymentReference}/refunds")
        {
            Content = JsonContent.Create(new { refund = new { description = "Order cancelled" } }),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // Idempotent per ADR-0018: a repeat refund of an already-refunded payment must
        // succeed rather than fail the cancellation flow.
        if (body.Contains("ALREADY_REFUNDED", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("REFUND_IS_PENDING", StringComparison.OrdinalIgnoreCase))
            return;

        throw new InvalidOperationException($"PayU refund request failed ({(int)response.StatusCode}): {body}");
    }

    private static string ToGrosze(decimal amount) =>
        ((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            return _cachedAccessToken;

        await TokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
                return _cachedAccessToken;

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
            };

            using var response = await _httpClient.PostAsync(
                "/pl/standard/user/oauth/authorize",
                new FormUrlEncodedContent(form),
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<PayUAuthTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("PayU did not return an OAuth access token.");

            _cachedAccessToken = token.AccessToken;
            // Refresh a little before actual expiry to avoid racing a token that just expired.
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn - 30, 0));

            return _cachedAccessToken;
        }
        finally
        {
            TokenLock.Release();
        }
    }
}
