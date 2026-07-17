namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Local time-of-day interval (no date), used to describe an opening period within a
/// day of the week (domain-model.md 2.6).
/// </summary>
public sealed class TimeRange : IEquatable<TimeRange>
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            throw new ArgumentException("End time must be after start time.", nameof(end));

        Start = start;
        End = end;
    }

    public bool Contains(TimeOnly time) => time >= Start && time <= End;

    public bool Equals(TimeRange? other) =>
        other is not null && Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => Equals(obj as TimeRange);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{Start:HH:mm}-{End:HH:mm}";

    public static bool operator ==(TimeRange? left, TimeRange? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(TimeRange? left, TimeRange? right) => !(left == right);
}
