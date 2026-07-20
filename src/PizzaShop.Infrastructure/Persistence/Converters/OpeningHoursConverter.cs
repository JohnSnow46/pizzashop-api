using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="OpeningHours"/> as a single <c>jsonb</c> column — the day-of-week to
/// time-range schedule is too complex for scalar columns and this is a Value Object, not an
/// entity worth its own table (ADR-0020, infrastructure-layer.md 2.2). Serializes to a
/// persistence DTO (<see cref="ScheduleDto"/>) and reconstructs via <see cref="OpeningHours"/>'s
/// public constructor.
/// </summary>
public sealed class OpeningHoursConverter : ValueConverter<OpeningHours, string>
{
    public OpeningHoursConverter()
        : base(
            openingHours => Serialize(openingHours),
            json => Deserialize(json))
    {
    }

    private static string Serialize(OpeningHours openingHours)
    {
        var schedule = Enum.GetValues<DayOfWeek>().ToDictionary(
            day => (int)day,
            day => openingHours.RangesFor(day).Select(r => new TimeRangeDto(r.Start, r.End)).ToList());

        return JsonSerializer.Serialize(schedule);
    }

    private static OpeningHours Deserialize(string json)
    {
        var schedule = JsonSerializer.Deserialize<Dictionary<int, List<TimeRangeDto>>>(json) ?? new();

        var days = schedule.ToDictionary(
            kv => (DayOfWeek)kv.Key,
            kv => (IReadOnlyList<TimeRange>)kv.Value.Select(t => new TimeRange(t.Start, t.End)).ToList());

        return new OpeningHours(days);
    }

    private sealed record TimeRangeDto(TimeOnly Start, TimeOnly End);
}

/// <summary>
/// Change-tracking comparer for the <c>jsonb</c>-converted <see cref="OpeningHours"/> column
/// — uses the VO's own value equality instead of reference equality (infrastructure-layer.md
/// 2.2). <see cref="OpeningHours"/> is immutable (every update replaces the whole reference,
/// see <c>Restaurant.UpdateOpeningHours</c>), so the instance itself is a safe snapshot.
/// </summary>
public sealed class OpeningHoursValueComparer : ValueComparer<OpeningHours>
{
    public OpeningHoursValueComparer()
        : base(
            (left, right) => left!.Equals(right),
            openingHours => openingHours.GetHashCode(),
            openingHours => openingHours)
    {
    }
}
