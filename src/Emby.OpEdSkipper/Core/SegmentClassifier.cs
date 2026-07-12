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

    [GeneratedRegex(@"(^|[^\p{L}\p{N}])(opening|intro|op|op\d+|\u7247\u5934|\u7247\u982d|\u30aa\u30fc\u30d7\u30cb\u30f3\u30b0)([^\p{L}\p{N}]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex OpeningRegex();

    [GeneratedRegex(@"(^|[^\p{L}\p{N}])(ending|outro|ed|ed\d+|credits?|end credits|\u7247\u5c3e|\u7247\u5c3e\u66f2|\u30a8\u30f3\u30c7\u30a3\u30f3\u30b0)([^\p{L}\p{N}]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex EndingRegex();
}
