using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Emby.OpEdSkipper.Core;
using Emby.OpEdSkipper.Storage;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Emby.OpEdSkipper;

public sealed class ServerEntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly SkipDecisionEngine _engine;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSeekBySession = new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    public ServerEntryPoint(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;

        var plugin = Plugin.Instance;
        var statePath = plugin is null
            ? Path.Combine(AppContext.BaseDirectory, "op-ed-skipper-seen-segments.json")
            : plugin.StateFilePath;
        _engine = new SkipDecisionEngine(new JsonSkipStateStore(statePath));
    }

    public void Run()
    {
        _sessionManager.PlaybackStart += OnPlaybackEvent;
        _sessionManager.PlaybackProgress += OnPlaybackEvent;
        _sessionManager.PlaybackStopped += OnPlaybackEvent;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sessionManager.PlaybackStart -= OnPlaybackEvent;
        _sessionManager.PlaybackProgress -= OnPlaybackEvent;
        _sessionManager.PlaybackStopped -= OnPlaybackEvent;
    }

    private async void OnPlaybackEvent(object? sender, EventArgs args)
    {
        try
        {
            await HandlePlaybackEvent(args, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Emby event handlers should not throw back into the server pipeline.
        }
    }

    private async Task HandlePlaybackEvent(EventArgs args, CancellationToken cancellationToken)
    {
        var snapshot = PlaybackEventSnapshot.From(args);
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!configuration.Enabled || snapshot.PositionTicks is null)
        {
            return;
        }

        if (snapshot.IsStoppedEvent)
        {
            Observe(configuration, snapshot);
            return;
        }

        if (snapshot.IsPaused)
        {
            return;
        }

        var sessionId = snapshot.SessionId;
        var userId = snapshot.UserId;
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var scopeId = GetScopeId(snapshot.Item);
        var runtimeTicks = GetTicks(snapshot.Item, "RunTimeTicks") ?? GetTicks(snapshot.MediaInfo, "RunTimeTicks");
        var segments = SegmentReader.ReadSegments(snapshot.Item, runtimeTicks);
        if (segments.Count == 0)
        {
            return;
        }

        var decision = _engine.Evaluate(
            configuration,
            userId,
            scopeId,
            snapshot.PositionTicks.Value,
            segments,
            DateTimeOffset.UtcNow);

        if (!decision.ShouldSeek || decision.SeekTicks is null)
        {
            return;
        }

        if (!CanSendSeek(sessionId, decision.MatchedSegment))
        {
            return;
        }

        var request = new PlaystateRequest
        {
            Command = PlaystateCommand.Seek,
            SeekPositionTicks = decision.SeekTicks.Value
        };

        await _sessionManager
            .SendPlaystateCommand(userId, sessionId, request, cancellationToken)
            .ConfigureAwait(false);
    }

    private void Observe(PluginConfiguration configuration, PlaybackEventSnapshot snapshot)
    {
        if (!configuration.Enabled || snapshot.PositionTicks is null)
        {
            return;
        }

        var userId = snapshot.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var scopeId = GetScopeId(snapshot.Item);
        var runtimeTicks = GetTicks(snapshot.Item, "RunTimeTicks") ?? GetTicks(snapshot.MediaInfo, "RunTimeTicks");
        var segments = SegmentReader.ReadSegments(snapshot.Item, runtimeTicks);

        _engine.ObserveProgress(
            configuration,
            userId,
            scopeId,
            snapshot.PositionTicks.Value,
            segments,
            DateTimeOffset.UtcNow);
    }

    private bool CanSendSeek(string sessionId, SkipSegment? segment)
    {
        if (segment is null)
        {
            return false;
        }

        var key = $"{sessionId}|{segment.Type}|{segment.StartTicks}|{segment.EndTicks}";
        var now = DateTimeOffset.UtcNow;
        var last = _lastSeekBySession.GetOrAdd(key, now);
        if (last != now && now - last < TimeSpan.FromSeconds(10))
        {
            return false;
        }

        _lastSeekBySession[key] = now;
        return true;
    }

    private static string GetScopeId(object? item)
    {
        var seriesId = Convert.ToString(GetMemberValue(item, "SeriesId"), CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(seriesId))
        {
            return $"series:{seriesId}";
        }

        var seasonId = Convert.ToString(GetMemberValue(item, "SeasonId"), CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(seasonId))
        {
            return $"season:{seasonId}";
        }

        var parentId = Convert.ToString(GetMemberValue(item, "ParentId"), CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            return $"parent:{parentId}";
        }

        var itemId = Convert.ToString(GetMemberValue(item, "Id"), CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(itemId) ? "unknown" : $"item:{itemId}";
    }

    private static long? GetTicks(object? item, string name)
    {
        var value = GetMemberValue(item, name);
        return value switch
        {
            null => null,
            long longValue => longValue,
            int intValue => intValue,
            TimeSpan timeSpan => timeSpan.Ticks,
            _ when long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) => ticks,
            _ => null
        };
    }

    private static object? GetMemberValue(object? instance, string name)
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

    private sealed record PlaybackEventSnapshot(
        string EventName,
        string SessionId,
        string UserId,
        long? PositionTicks,
        bool IsPaused,
        object? Item,
        object? MediaInfo)
    {
        public bool IsStoppedEvent => EventName.Contains("stop", StringComparison.OrdinalIgnoreCase);

        public static PlaybackEventSnapshot From(EventArgs args)
        {
            var session = GetMemberValue(args, "Session");
            var users = GetMemberValue(args, "Users") as System.Collections.IEnumerable;
            var firstUser = users?.Cast<object>().FirstOrDefault();

            var sessionId = Convert.ToString(GetMemberValue(session, "Id"), CultureInfo.InvariantCulture) ?? string.Empty;
            var sessionUserId = Convert.ToString(GetMemberValue(session, "UserId"), CultureInfo.InvariantCulture);
            var eventUserId = Convert.ToString(GetMemberValue(args, "UserId"), CultureInfo.InvariantCulture);
            var firstUserId = Convert.ToString(GetMemberValue(firstUser, "Id"), CultureInfo.InvariantCulture);

            return new PlaybackEventSnapshot(
                args.GetType().Name,
                sessionId,
                FirstNonEmpty(sessionUserId, eventUserId, firstUserId),
                GetTicks(args, "PlaybackPositionTicks") ?? GetTicks(args, "PositionTicks"),
                Convert.ToBoolean(GetMemberValue(args, "IsPaused") ?? false, CultureInfo.InvariantCulture),
                GetMemberValue(args, "Item"),
                GetMemberValue(args, "MediaInfo"));
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
