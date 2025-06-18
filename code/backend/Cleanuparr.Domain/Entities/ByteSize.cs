using System.Globalization;

namespace Cleanuparr.Domain.Entities;

public readonly struct ByteSize : IComparable<ByteSize>, IEquatable<ByteSize>
{
    public long Bytes { get; }

    private const long BytesPerKB = 1024;
    private const long BytesPerMB = 1024 * 1024;
    private const long BytesPerGB = 1024 * 1024 * 1024;

    public ByteSize(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "bytes can not be negative");
        }

        Bytes = bytes;
    }

    public static ByteSize FromKilobytes(double kilobytes) => new((long)(kilobytes * BytesPerKB));
    public static ByteSize FromMegabytes(double megabytes) => new((long)(megabytes * BytesPerMB));
    public static ByteSize FromGigabytes(double gigabytes) => new((long)(gigabytes * BytesPerGB));

    public static ByteSize Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentNullException(nameof(input));
        }

        input = input.Trim().ToUpperInvariant();
        double value;
        if (input.EndsWith("KB", StringComparison.InvariantCultureIgnoreCase))
        {
            value = double.Parse(input[..^2], CultureInfo.InvariantCulture);
            return FromKilobytes(value);
        }

        if (input.EndsWith("MB", StringComparison.InvariantCultureIgnoreCase))
        {
            value = double.Parse(input[..^2], CultureInfo.InvariantCulture);
            return FromMegabytes(value);
        }

        if (input.EndsWith("GB", StringComparison.InvariantCultureIgnoreCase))
        {
            value = double.Parse(input[..^2], CultureInfo.InvariantCulture);
            return FromGigabytes(value);
        }

        throw new FormatException("invalid size format | only KB, MB and GB are supported");
    }
    
    public static bool TryParse(string? input, out ByteSize? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim().ToUpperInvariant();

        if (input.EndsWith("KB", StringComparison.InvariantCultureIgnoreCase) &&
            double.TryParse(input[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out double kb))
        {
            result = FromKilobytes(kb);
            return true;
        }

        if (input.EndsWith("MB", StringComparison.InvariantCultureIgnoreCase) &&
            double.TryParse(input[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out double mb))
        {
            result = FromMegabytes(mb);
            return true;
        }

        if (input.EndsWith("GB", StringComparison.InvariantCultureIgnoreCase) &&
            double.TryParse(input[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out double gb))
        {
            result = FromGigabytes(gb);
            return true;
        }

        return false;
    }

    public override string ToString() =>
        Bytes switch
        {
            >= BytesPerGB => $"{Bytes / (double)BytesPerGB:0.##} GB",
            >= BytesPerMB => $"{Bytes / (double)BytesPerMB:0.##} MB",
            _ => $"{Bytes / (double)BytesPerKB:0.##} KB"
        };

    public int CompareTo(ByteSize other) => Bytes.CompareTo(other.Bytes);
    public bool Equals(ByteSize other) => Bytes == other.Bytes;

    public override bool Equals(object? obj) => obj is ByteSize other && Equals(other);
    public override int GetHashCode() => Bytes.GetHashCode();

    public static bool operator ==(ByteSize left, ByteSize right) => left.Equals(right);
    public static bool operator !=(ByteSize left, ByteSize right) => !(left == right);
    public static bool operator <(ByteSize left, ByteSize right) => left.Bytes < right.Bytes;
    public static bool operator >(ByteSize left, ByteSize right) => left.Bytes > right.Bytes;
    public static bool operator <=(ByteSize left, ByteSize right) => left.Bytes <= right.Bytes;
    public static bool operator >=(ByteSize left, ByteSize right) => left.Bytes >= right.Bytes;

    public static ByteSize operator +(ByteSize left, ByteSize right) => new(left.Bytes + right.Bytes);
    public static ByteSize operator -(ByteSize left, ByteSize right) => new(Math.Max(left.Bytes - right.Bytes, 0));
}