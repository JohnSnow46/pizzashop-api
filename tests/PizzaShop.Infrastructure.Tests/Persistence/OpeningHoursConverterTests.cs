using FluentAssertions;
using PizzaShop.Domain.ValueObjects;
using PizzaShop.Infrastructure.Persistence.Converters;

namespace PizzaShop.Infrastructure.Tests.Persistence;

/// <summary>Pure unit tests for <see cref="OpeningHoursConverter"/> — no database required.</summary>
public sealed class OpeningHoursConverterTests
{
    private readonly OpeningHoursConverter _converter = new();
    private readonly OpeningHoursValueComparer _comparer = new();

    [Fact]
    public void RoundTrip_PreservesScheduleAcrossAllDays()
    {
        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(10, 0), new TimeOnly(22, 0)) },
            [DayOfWeek.Tuesday] = new List<TimeRange>
            {
                new(new TimeOnly(10, 0), new TimeOnly(14, 0)),
                new(new TimeOnly(16, 0), new TimeOnly(22, 0)),
            },
        };
        var original = new OpeningHours(schedule);

        var json = (string?)_converter.ConvertToProvider(original);
        var restored = (OpeningHours?)_converter.ConvertFromProvider(json);

        restored.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_ClosedDayStaysEmpty()
    {
        var original = new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(9, 0), new TimeOnly(17, 0)) },
        });

        var json = (string?)_converter.ConvertToProvider(original);
        var restored = (OpeningHours?)_converter.ConvertFromProvider(json);

        restored!.RangesFor(DayOfWeek.Sunday).Should().BeEmpty();
    }

    [Fact]
    public void ValueComparer_EqualSchedules_AreEqual()
    {
        var scheduleA = new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(9, 0), new TimeOnly(17, 0)) },
        });
        var scheduleB = new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(9, 0), new TimeOnly(17, 0)) },
        });

        _comparer.Equals(scheduleA, scheduleB).Should().BeTrue();
        _comparer.GetHashCode(scheduleA).Should().Be(_comparer.GetHashCode(scheduleB));
    }
}
