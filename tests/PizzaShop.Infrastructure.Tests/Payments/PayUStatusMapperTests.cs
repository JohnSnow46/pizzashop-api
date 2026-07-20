using FluentAssertions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Enums;
using PizzaShop.Infrastructure.Payments.PayU;

namespace PizzaShop.Infrastructure.Tests.Payments;

/// <summary>Pure unit tests for <see cref="PayUStatusMapper"/> — no network/Docker required.</summary>
public sealed class PayUStatusMapperTests
{
    [Theory]
    [InlineData("PENDING", PaymentStatus.Pending)]
    [InlineData("WAITING_FOR_CONFIRMATION", PaymentStatus.Authorized)]
    [InlineData("COMPLETED", PaymentStatus.Paid)]
    [InlineData("CANCELED", PaymentStatus.Failed)]
    [InlineData("REJECTED", PaymentStatus.Failed)]
    public void Map_KnownPayUStatus_ReturnsExpectedPaymentStatus(string payUStatus, PaymentStatus expected)
    {
        var result = PayUStatusMapper.Map(payUStatus);

        result.Should().Be(expected);
    }

    [Fact]
    public void Map_UnrecognizedStatus_ThrowsInvalidPaymentNotificationException()
    {
        var act = () => PayUStatusMapper.Map("SOME_UNKNOWN_STATUS");

        act.Should().Throw<InvalidPaymentNotificationException>();
    }
}
