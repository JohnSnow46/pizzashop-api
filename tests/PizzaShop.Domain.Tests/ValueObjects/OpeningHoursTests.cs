using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class OpeningHoursTests
{
    private static OpeningHours MondayWithLunchBreak() => new(
        new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange>
            {
                new(new TimeOnly(10, 0), new TimeOnly(14, 0)),
                new(new TimeOnly(15, 0), new TimeOnly(22, 0)),
            },
        });

    [Fact]
    public void IsOpenAt_WithinConfiguredRange_ReturnsTrue()
    {
        var hours = MondayWithLunchBreak();
        var monday1200Utc = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero); // a Monday

        hours.IsOpenAt(monday1200Utc, TimeZoneInfo.Utc).Should().BeTrue();
    }

    [Fact]
    public void IsOpenAt_DuringLunchBreak_ReturnsFalse()
    {
        var hours = MondayWithLunchBreak();
        var monday1430Utc = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);

        hours.IsOpenAt(monday1430Utc, TimeZoneInfo.Utc).Should().BeFalse();
    }

    [Fact]
    public void IsOpenAt_SecondRangeOfSameDay_ReturnsTrue()
    {
        var hours = MondayWithLunchBreak();
        var monday1600Utc = new DateTimeOffset(2024, 1, 1, 16, 0, 0, TimeSpan.Zero);

        hours.IsOpenAt(monday1600Utc, TimeZoneInfo.Utc).Should().BeTrue();
    }

    [Fact]
    public void IsOpenAt_DayNotInSchedule_ReturnsFalse()
    {
        var hours = MondayWithLunchBreak();
        var tuesday1200Utc = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);

        hours.IsOpenAt(tuesday1200Utc, TimeZoneInfo.Utc).Should().BeFalse();
    }

    [Fact]
    public void RangesFor_DayWithoutEntries_ReturnsEmpty()
    {
        var hours = MondayWithLunchBreak();

        hours.RangesFor(DayOfWeek.Sunday).Should().BeEmpty();
    }
}
