using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class TimeRangeTests
{
    [Fact]
    public void Constructor_EndBeforeStart_ThrowsArgumentException()
    {
        var act = () => new TimeRange(new TimeOnly(12, 0), new TimeOnly(10, 0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EndEqualsStart_ThrowsArgumentException()
    {
        var act = () => new TimeRange(new TimeOnly(12, 0), new TimeOnly(12, 0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contains_TimeWithinRange_ReturnsTrue()
    {
        var range = new TimeRange(new TimeOnly(10, 0), new TimeOnly(14, 0));

        range.Contains(new TimeOnly(12, 0)).Should().BeTrue();
    }

    [Fact]
    public void Contains_TimeOutsideRange_ReturnsFalse()
    {
        var range = new TimeRange(new TimeOnly(10, 0), new TimeOnly(14, 0));

        range.Contains(new TimeOnly(15, 0)).Should().BeFalse();
    }

    [Fact]
    public void Contains_BoundaryValues_ReturnsTrue()
    {
        var range = new TimeRange(new TimeOnly(10, 0), new TimeOnly(14, 0));

        range.Contains(new TimeOnly(10, 0)).Should().BeTrue();
        range.Contains(new TimeOnly(14, 0)).Should().BeTrue();
    }
}
