namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Weekly opening schedule: for each day of week, zero or more <see cref="TimeRange"/>
/// entries (supports splits, e.g. a lunch break). A day absent from the schedule (or
/// mapped to an empty list) is closed all day (domain-model.md 2.6).
/// </summary>
public sealed class OpeningHours : IEquatable<OpeningHours>
{
    private readonly Dictionary<DayOfWeek, IReadOnlyList<TimeRange>> _schedule;

    public OpeningHours(IReadOnlyDictionary<DayOfWeek, IReadOnlyList<TimeRange>> schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        _schedule = schedule.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public IReadOnlyList<TimeRange> RangesFor(DayOfWeek day) =>
        _schedule.TryGetValue(day, out var ranges) ? ranges : Array.Empty<TimeRange>();

    /// <summary>
    /// Whether the restaurant is open at the given instant, converted to the
    /// restaurant's local time zone (ADR-0010).
    /// </summary>
    public bool IsOpenAt(DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var local = TimeZoneInfo.ConvertTime(instant, timeZone);
        var time = TimeOnly.FromDateTime(local.DateTime);

        return RangesFor(local.DayOfWeek).Any(range => range.Contains(time));
    }

    public bool Equals(OpeningHours? other)
    {
        if (other is null)
            return false;

        var days = Enum.GetValues<DayOfWeek>();
        return days.All(day => RangesFor(day).SequenceEqual(other.RangesFor(day)));
    }

    public override bool Equals(object? obj) => Equals(obj as OpeningHours);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            hash.Add(day);
            foreach (var range in RangesFor(day))
                hash.Add(range);
        }

        return hash.ToHashCode();
    }
}
