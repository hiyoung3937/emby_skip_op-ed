namespace Emby.OpEdSkipper.Core;

public sealed record PlaybackDecision(
    bool ShouldSeek,
    long? SeekTicks,
    SkipSegment? MatchedSegment,
    string Reason)
{
    public static PlaybackDecision None(string reason)
    {
        return new PlaybackDecision(false, null, null, reason);
    }

    public static PlaybackDecision Seek(SkipSegment segment, long seekTicks, string reason)
    {
        return new PlaybackDecision(true, seekTicks, segment, reason);
    }
}
