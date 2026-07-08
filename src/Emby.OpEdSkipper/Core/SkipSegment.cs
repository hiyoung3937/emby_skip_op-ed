using System.Globalization;
using System.Text;

namespace Emby.OpEdSkipper.Core;

public sealed record SkipSegment(
    SegmentType Type,
    string Name,
    long StartTicks,
    long EndTicks)
{
    public long DurationTicks => Math.Max(0, EndTicks - StartTicks);

    public bool Contains(long positionTicks)
    {
        return positionTicks >= StartTicks && positionTicks < EndTicks;
    }

    public string BuildSignature()
    {
        var normalizedName = Normalize(Name);
        var roundedStartSeconds = TimeSpan.FromTicks(StartTicks).TotalSeconds;
        var roundedEndSeconds = TimeSpan.FromTicks(EndTicks).TotalSeconds;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Type}:{Math.Round(roundedStartSeconds)}:{Math.Round(roundedEndSeconds)}:{normalizedName}");
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unnamed";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.Length == 0 ? "unnamed" : builder.ToString();
    }
}
