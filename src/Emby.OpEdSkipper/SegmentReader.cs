using System.Globalization;
using System.Reflection;
using Emby.OpEdSkipper.Core;

namespace Emby.OpEdSkipper;

public static class SegmentReader
{
    public static IReadOnlyList<SkipSegment> ReadSegments(object? item, long? runtimeTicks)
    {
        if (item is null)
        {
            return Array.Empty<SkipSegment>();
        }

        var chapters = ReadChapters(item)
            .OrderBy(chapter => chapter.StartTicks)
            .ToArray();

        var segments = new List<SkipSegment>();
        for (var index = 0; index < chapters.Length; index++)
        {
            var chapter = chapters[index];
            var type = SegmentClassifier.Classify(chapter.Name);
            if (type is null)
            {
                continue;
            }

            var endTicks = chapter.EndTicks
                ?? GetNextStartTicks(chapters, index)
                ?? runtimeTicks;

            if (endTicks is null || endTicks <= chapter.StartTicks)
            {
                continue;
            }

            segments.Add(new SkipSegment(type.Value, chapter.Name, chapter.StartTicks, endTicks.Value));
        }

        foreach (var marker in ReadMarkers(item))
        {
            var type = SegmentClassifier.Classify(marker.Name);
            if (type is null || marker.EndTicks <= marker.StartTicks)
            {
                continue;
            }

            segments.Add(new SkipSegment(type.Value, marker.Name, marker.StartTicks, marker.EndTicks));
        }

        return segments
            .GroupBy(segment => $"{segment.Type}:{segment.StartTicks}:{segment.EndTicks}:{segment.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(segment => segment.StartTicks)
            .ToArray();
    }

    private static long? GetNextStartTicks(IReadOnlyList<RawChapter> chapters, int currentIndex)
    {
        for (var index = currentIndex + 1; index < chapters.Count; index++)
        {
            if (chapters[index].StartTicks > chapters[currentIndex].StartTicks)
            {
                return chapters[index].StartTicks;
            }
        }

        return null;
    }

    private static IEnumerable<RawChapter> ReadChapters(object item)
    {
        var chapterSources = new[]
        {
            TryInvoke(item, "GetChapters"),
            TryGetValue(item, "Chapters"),
            TryGetValue(item, "ChapterInfos")
        };

        foreach (var source in chapterSources)
        {
            if (source is not System.Collections.IEnumerable enumerable || source is string)
            {
                continue;
            }

            foreach (var chapter in enumerable)
            {
                var name = Convert.ToString(TryGetValue(chapter, "Name"), CultureInfo.InvariantCulture)
                    ?? Convert.ToString(TryGetValue(chapter, "MarkerType"), CultureInfo.InvariantCulture)
                    ?? string.Empty;
                var startTicks = ToTicks(
                    TryGetValue(chapter, "StartPositionTicks")
                    ?? TryGetValue(chapter, "StartTicks")
                    ?? TryGetValue(chapter, "PositionTicks"));
                var endTicks = ToTicks(
                    TryGetValue(chapter, "EndPositionTicks")
                    ?? TryGetValue(chapter, "EndTicks"));

                if (startTicks is null)
                {
                    continue;
                }

                yield return new RawChapter(name, startTicks.Value, endTicks);
            }
        }
    }

    private static IEnumerable<RawMarker> ReadMarkers(object item)
    {
        var markerSources = new[]
        {
            TryGetValue(item, "IntroMarkers"),
            TryGetValue(item, "MediaSegments"),
            TryGetValue(item, "Markers")
        };

        foreach (var source in markerSources)
        {
            if (source is not System.Collections.IEnumerable enumerable || source is string)
            {
                continue;
            }

            foreach (var marker in enumerable)
            {
                var name = Convert.ToString(TryGetValue(marker, "Name"), CultureInfo.InvariantCulture)
                    ?? Convert.ToString(TryGetValue(marker, "Type"), CultureInfo.InvariantCulture)
                    ?? Convert.ToString(TryGetValue(marker, "MarkerType"), CultureInfo.InvariantCulture)
                    ?? string.Empty;
                var startTicks = ToTicks(TryGetValue(marker, "StartPositionTicks") ?? TryGetValue(marker, "StartTicks"));
                var endTicks = ToTicks(TryGetValue(marker, "EndPositionTicks") ?? TryGetValue(marker, "EndTicks"));

                if (startTicks is null || endTicks is null)
                {
                    continue;
                }

                yield return new RawMarker(name, startTicks.Value, endTicks.Value);
            }
        }
    }

    private static object? TryGetValue(object? instance, string name)
    {
        if (instance is null)
        {
            return null;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
        var type = instance.GetType();
        var property = type.GetProperty(name, flags);
        if (property is not null)
        {
            return property.GetValue(instance);
        }

        var field = type.GetField(name, flags);
        return field?.GetValue(instance);
    }

    private static object? TryInvoke(object? instance, string name)
    {
        if (instance is null)
        {
            return null;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
        var method = instance.GetType().GetMethod(name, flags, Type.EmptyTypes);
        return method?.Invoke(instance, Array.Empty<object>());
    }

    private static long? ToTicks(object? value)
    {
        return value switch
        {
            null => null,
            long longValue => longValue,
            int intValue => intValue,
            double doubleValue => (long)doubleValue,
            TimeSpan timeSpan => timeSpan.Ticks,
            _ when long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) => ticks,
            _ => null
        };
    }

    private sealed record RawChapter(string Name, long StartTicks, long? EndTicks);

    private sealed record RawMarker(string Name, long StartTicks, long EndTicks);
}
