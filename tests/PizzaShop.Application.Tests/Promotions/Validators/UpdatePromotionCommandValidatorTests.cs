using FluentAssertions;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Application.Promotions.Validators;

namespace PizzaShop.Application.Tests.Promotions.Validators;

public class UpdatePromotionCommandValidatorTests
{
    private readonly UpdatePromotionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPromotionId_HasErrorForPromotionId()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.Empty, IsActive: false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePromotionCommand.PromotionId));
    }

    [Fact]
    public void Validate_ValidFromAndValidToBothSupplied_HasNoErrors()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(
            Guid.NewGuid(),
            IsActive: false,
            ValidFrom: DateTimeOffset.UtcNow,
            ValidTo: DateTimeOffset.UtcNow.AddDays(10)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OnlyValidFromSupplied_HasError()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(
            Guid.NewGuid(),
            IsActive: false,
            ValidFrom: DateTimeOffset.UtcNow));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_OnlyValidToSupplied_HasError()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(
            Guid.NewGuid(),
            IsActive: false,
            ValidTo: DateTimeOffset.UtcNow));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidToNotAfterValidFrom_HasError()
    {
        var now = DateTimeOffset.UtcNow;
        var result = _validator.Validate(new UpdatePromotionCommand(
            Guid.NewGuid(),
            IsActive: false,
            ValidFrom: now,
            ValidTo: now));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeValue_HasErrorForValue()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false, Value: -1m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePromotionCommand.Value));
    }

    [Fact]
    public void Validate_NonNegativeValue_HasNoErrors()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false, Value: 15m));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_UsageLimitZeroOrLess_HasErrorForUsageLimit(int usageLimit)
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false, UsageLimit: usageLimit));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePromotionCommand.UsageLimit));
    }

    [Fact]
    public void Validate_PositiveUsageLimit_HasNoErrors()
    {
        var result = _validator.Validate(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false, UsageLimit: 3));

        result.IsValid.Should().BeTrue();
    }
}
