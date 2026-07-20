using System.Text.Json.Serialization;

namespace PizzaShop.Infrastructure.Payments.PayU;

/// <summary>
/// Internal wire DTOs for PayU's REST API (ADR-0022) — never exposed outside
/// <see cref="PayUPaymentGateway"/>.
/// </summary>
internal sealed record PayUAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

internal sealed record PayUCreateOrderRequest(
    [property: JsonPropertyName("notifyUrl")] string NotifyUrl,
    [property: JsonPropertyName("continueUrl")] string ContinueUrl,
    [property: JsonPropertyName("customerIp")] string CustomerIp,
    [property: JsonPropertyName("merchantPosId")] string MerchantPosId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("totalAmount")] string TotalAmount,
    [property: JsonPropertyName("extOrderId")] string ExtOrderId,
    [property: JsonPropertyName("buyer")] PayUBuyer? Buyer,
    [property: JsonPropertyName("products")] IReadOnlyList<PayUProduct> Products);

internal sealed record PayUBuyer([property: JsonPropertyName("email")] string Email);

internal sealed record PayUProduct(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unitPrice")] string UnitPrice,
    [property: JsonPropertyName("quantity")] string Quantity);

internal sealed record PayUCreateOrderResponse(
    [property: JsonPropertyName("redirectUri")] string? RedirectUri,
    [property: JsonPropertyName("orderId")] string? OrderId,
    [property: JsonPropertyName("status")] PayUStatusDto? Status);

internal sealed record PayUStatusDto([property: JsonPropertyName("statusCode")] string? StatusCode);

internal sealed record PayUNotificationPayload([property: JsonPropertyName("order")] PayUNotificationOrder? Order);

internal sealed record PayUNotificationOrder(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("extOrderId")] string? ExtOrderId,
    [property: JsonPropertyName("status")] string Status);
