using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using PizzaShop.Infrastructure.Payments.PayU;

namespace PizzaShop.Infrastructure.Tests.Payments;

/// <summary>Pure unit tests for <see cref="PayUSignatureVerifier"/> — no network/Docker required.</summary>
public sealed class PayUSignatureVerifierTests
{
    private const string SecondKey = "test-second-key";

    [Fact]
    public void Verify_ValidMd5Signature_ReturnsTrue()
    {
        const string body = "{\"order\":{\"orderId\":\"ABC\",\"status\":\"COMPLETED\"}}";
        var signature = Md5Hex(body + SecondKey);
        var header = $"signature={signature};algorithm=MD5;sender=checkout";

        var result = PayUSignatureVerifier.Verify(body, header, SecondKey);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedBody_ReturnsFalse()
    {
        const string originalBody = "{\"order\":{\"orderId\":\"ABC\",\"status\":\"COMPLETED\"}}";
        const string tamperedBody = "{\"order\":{\"orderId\":\"ABC\",\"status\":\"CANCELED\"}}";
        var signature = Md5Hex(originalBody + SecondKey);
        var header = $"signature={signature};algorithm=MD5;sender=checkout";

        var result = PayUSignatureVerifier.Verify(tamperedBody, header, SecondKey);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongSecondKey_ReturnsFalse()
    {
        const string body = "{\"order\":{\"orderId\":\"ABC\",\"status\":\"COMPLETED\"}}";
        var signature = Md5Hex(body + SecondKey);
        var header = $"signature={signature};algorithm=MD5;sender=checkout";

        var result = PayUSignatureVerifier.Verify(body, header, "different-key");

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_MissingSignatureField_ReturnsFalse()
    {
        var result = PayUSignatureVerifier.Verify("body", "algorithm=MD5;sender=checkout", SecondKey);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptyHeader_ReturnsFalse()
    {
        var result = PayUSignatureVerifier.Verify("body", string.Empty, SecondKey);

        result.Should().BeFalse();
    }

    private static string Md5Hex(string input) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
