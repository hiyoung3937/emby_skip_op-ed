using System.Text.RegularExpressions;

namespace Emby.OpEdSkipper.Core;

public static partial class SegmentClassifier
{
    public static SegmentType? Classify(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalized = NormalizeRegex().Replace(name.Trim().ToLowerInvariant(), " ");
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

        if (OpeningRegex().IsMatch(normalized))
        {
            return SegmentType.Opening;
        }

        if (EndingRegex().IsMatch(normalized))
        {
            return SegmentType.Ending;
        }

        return null;
    }

    [GeneratedRegex(@"[_\-\.\[\]\(\):]+")]
    private static partial Regex NormalizeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(^|\s)(opening|intro|op|片头|オープニング)(\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex OpeningRegex();

    [GeneratedRegex(@"(^|\s)(ending|outro|ed|片尾|エンディング)(\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex EndingRegex();
}
