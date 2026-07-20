using System.Security.Cryptography;
using System.Text;

namespace PizzaShop.Infrastructure.Payments.PayU;

/// <summary>
/// Verifies the <c>OpenPayU-Signature</c> header on incoming webhook notifications (ADR-0013/
/// ADR-0022) — the only protection on that endpoint, since it deliberately carries no JWT.
/// The header looks like <c>signature=&lt;hash&gt;;algorithm=MD5;sender=checkout</c>; the hash
/// is computed over the raw request body concatenated with PayU's second (signature) key.
/// </summary>
public static class PayUSignatureVerifier
{
    public static bool Verify(string rawBody, string signatureHeaderValue, string secondKey)
    {
        if (string.IsNullOrWhiteSpace(signatureHeaderValue) || string.IsNullOrWhiteSpace(secondKey))
            return false;

        var fields = ParseHeader(signatureHeaderValue);

        if (!fields.TryGetValue("signature", out var expectedSignature) || string.IsNullOrWhiteSpace(expectedSignature))
            return false;

        var algorithm = fields.TryGetValue("algorithm", out var value) ? value : "MD5";
        var computedSignature = ComputeHash(rawBody + secondKey, algorithm);

        return string.Equals(computedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> ParseHeader(string headerValue) =>
        headerValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);

    private static string ComputeHash(string input, string algorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(input);

        var hash = algorithm.Trim().ToUpperInvariant() switch
        {
            "SHA" or "SHA1" or "SHA-1" => SHA1.HashData(bytes),
            "SHA256" or "SHA-256" => SHA256.HashData(bytes),
            _ => MD5.HashData(bytes),
        };

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
