using Emby.OpEdSkipper;

namespace Emby.OpEdSkipper.Core;

public sealed class SkipDecisionEngine
{
    private readonly SkipStateStore _store;

    public SkipDecisionEngine(SkipStateStore store)
    {
        _store = store;
    }

    public PlaybackDecision Evaluate(
        PluginConfiguration configuration,
        string userId,
        string scopeId,
        long positionTicks,
        IReadOnlyList<SkipSegment> segments,
        DateTimeOffset now)
    {
        if (!configuration.Enabled)
        {
            return PlaybackDecision.None("Plugin disabled.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return PlaybackDecision.None("Missing user id.");
        }

        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return PlaybackDecision.None("Missing series or item scope.");
        }

        foreach (var segment in segments)
        {
            if (!IsEnabled(configuration, segment.Type))
            {
                continue;
            }

            if (segment.DurationTicks < TimeSpan.FromSeconds(configuration.MinimumSegmentSeconds).Ticks)
            {
                continue;
            }

            if (!segment.Contains(positionTicks))
            {
                continue;
            }

            var wasSeenBeforeThisEvent = _store.HasSeen(userId, scopeId, segment);
            var seenThresholdTicks = segment.StartTicks + (long)(segment.DurationTicks * configuration.SeenThresholdRatio);
            if (!wasSeenBeforeThisEvent && positionTicks >= seenThresholdTicks)
            {
                _store.MarkSeen(userId, scopeId, segment, now);
            }

            if (!wasSeenBeforeThisEvent)
            {
                return PlaybackDecision.None("Segment has not been watched before.");
            }

            var seekTicks = Math.Min(segment.EndTicks + TimeSpan.FromMilliseconds(configuration.SeekPastSegmentMilliseconds).Ticks, long.MaxValue);
            return PlaybackDecision.Seek(segment, seekTicks, "Segment has already been watched.");
        }

        return PlaybackDecision.None("No matching segment.");
    }

    public void ObserveProgress(
        PluginConfiguration configuration,
        string userId,
        string scopeId,
        long positionTicks,
        IReadOnlyList<SkipSegment> segments,
        DateTimeOffset now)
    {
        if (!configuration.Enabled || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(scopeId))
        {
            return;
        }

        foreach (var segment in segments)
        {
            if (!IsEnabled(configuration, segment.Type))
            {
                continue;
            }

            if (segment.DurationTicks < TimeSpan.FromSeconds(configuration.MinimumSegmentSeconds).Ticks)
            {
                continue;
            }

            var thresholdTicks = segment.StartTicks + (long)(segment.DurationTicks * configuration.SeenThresholdRatio);
            if (positionTicks >= thresholdTicks)
            {
                _store.MarkSeen(userId, scopeId, segment, now);
            }
        }
    }

    private static bool IsEnabled(PluginConfiguration configuration, SegmentType type)
    {
        return type switch
        {
            SegmentType.Opening => configuration.SkipOpenings,
            SegmentType.Ending => configuration.SkipEndings,
            _ => false
        };
    }
}
