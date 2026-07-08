namespace Emby.OpEdSkipper.Core;

public interface SkipStateStore
{
    bool HasSeen(string userId, string scopeId, SkipSegment segment);

    void MarkSeen(string userId, string scopeId, SkipSegment segment, DateTimeOffset seenAt);
}
