using System.Text.Json;
using Emby.OpEdSkipper.Core;

namespace Emby.OpEdSkipper.Storage;

public sealed class JsonSkipStateStore : SkipStateStore
{
    private readonly object _syncRoot = new();
    private readonly string _path;
    private StateFile? _state;

    public JsonSkipStateStore(string path)
    {
        _path = path;
    }

    public bool HasSeen(string userId, string scopeId, SkipSegment segment)
    {
        lock (_syncRoot)
        {
            var key = BuildKey(userId, scopeId, segment);
            return Load().SeenSegments.ContainsKey(key);
        }
    }

    public void MarkSeen(string userId, string scopeId, SkipSegment segment, DateTimeOffset seenAt)
    {
        lock (_syncRoot)
        {
            var state = Load();
            var key = BuildKey(userId, scopeId, segment);
            if (state.SeenSegments.TryGetValue(key, out var existing) && existing.SeenAt >= seenAt)
            {
                return;
            }

            state.SeenSegments[key] = new SeenSegmentRecord
            {
                UserId = userId,
                ScopeId = scopeId,
                SegmentType = segment.Type.ToString(),
                SegmentSignature = segment.BuildSignature(),
                SegmentName = segment.Name,
                StartTicks = segment.StartTicks,
                EndTicks = segment.EndTicks,
                SeenAt = seenAt
            };

            Save(state);
        }
    }

    private StateFile Load()
    {
        if (_state is not null)
        {
            return _state;
        }

        if (!File.Exists(_path))
        {
            _state = new StateFile();
            return _state;
        }

        try
        {
            var json = File.ReadAllText(_path);
            _state = JsonSerializer.Deserialize<StateFile>(json, JsonOptions()) ?? new StateFile();
        }
        catch
        {
            _state = new StateFile();
        }

        return _state;
    }

    private void Save(StateFile state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        var json = JsonSerializer.Serialize(state, JsonOptions());
        File.WriteAllText(_path, json);
    }

    private static string BuildKey(string userId, string scopeId, SkipSegment segment)
    {
        return $"{userId}|{scopeId}|{segment.Type}|{segment.BuildSignature()}";
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    private sealed class StateFile
    {
        public Dictionary<string, SeenSegmentRecord> SeenSegments { get; set; } = new();
    }

    private sealed class SeenSegmentRecord
    {
        public string UserId { get; set; } = string.Empty;

        public string ScopeId { get; set; } = string.Empty;

        public string SegmentType { get; set; } = string.Empty;

        public string SegmentSignature { get; set; } = string.Empty;

        public string SegmentName { get; set; } = string.Empty;

        public long StartTicks { get; set; }

        public long EndTicks { get; set; }

        public DateTimeOffset SeenAt { get; set; }
    }
}
