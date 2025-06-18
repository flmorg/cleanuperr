using System.Text;

namespace Cleanuparr.Domain.Entities;

public readonly struct SmartTimeSpan : IComparable<SmartTimeSpan>, IEquatable<SmartTimeSpan>
{
    public TimeSpan Time { get; }

    public SmartTimeSpan(TimeSpan time)
    {
        Time = time;
    }

    public override string ToString()
    {
        if (Time == TimeSpan.Zero)
        {
            return "0 seconds";
        }

        StringBuilder sb = new();

        if (Time.Days > 0)
        {
            sb.Append($"{Time.Days} day{(Time.Days > 1 ? "s" : "")} ");
        }

        if (Time.Hours > 0)
        {
            sb.Append($"{Time.Hours} hour{(Time.Hours > 1 ? "s" : "")} ");
        }

        if (Time.Minutes > 0)
        {
            sb.Append($"{Time.Minutes} minute{(Time.Minutes > 1 ? "s" : "")} ");
        }

        if (Time.Seconds > 0)
        {
            sb.Append($"{Time.Seconds} second{(Time.Seconds > 1 ? "s" : "")}");
        }

        return sb.ToString().TrimEnd();
    }

    public static SmartTimeSpan FromMinutes(double minutes) => new(TimeSpan.FromMinutes(minutes));
    public static SmartTimeSpan FromSeconds(double seconds) => new(TimeSpan.FromSeconds(seconds));
    public static SmartTimeSpan FromHours(double hours) => new(TimeSpan.FromHours(hours));
    public static SmartTimeSpan FromDays(double days) => new(TimeSpan.FromDays(days));

    public int CompareTo(SmartTimeSpan other) => Time.CompareTo(other.Time);
    public bool Equals(SmartTimeSpan other) => Time.Equals(other.Time);

    public override bool Equals(object? obj) => obj is SmartTimeSpan other && Equals(other);
    public override int GetHashCode() => Time.GetHashCode();

    public static bool operator ==(SmartTimeSpan left, SmartTimeSpan right) => left.Equals(right);
    public static bool operator !=(SmartTimeSpan left, SmartTimeSpan right) => !left.Equals(right);
    public static bool operator <(SmartTimeSpan left, SmartTimeSpan right) => left.Time < right.Time;
    public static bool operator >(SmartTimeSpan left, SmartTimeSpan right) => left.Time > right.Time;
    public static bool operator <=(SmartTimeSpan left, SmartTimeSpan right) => left.Time <= right.Time;
    public static bool operator >=(SmartTimeSpan left, SmartTimeSpan right) => left.Time >= right.Time;

    public static SmartTimeSpan operator +(SmartTimeSpan left, SmartTimeSpan right) => new(left.Time + right.Time);
    public static SmartTimeSpan operator -(SmartTimeSpan left, SmartTimeSpan right) => new(left.Time - right.Time);
}