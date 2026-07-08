using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Session;

public interface ISessionManager
{
    event EventHandler<EventArgs> PlaybackStart;

    event EventHandler<EventArgs> PlaybackProgress;

    event EventHandler<EventArgs> PlaybackStopped;

    Task SendPlaystateCommand(string controllingUserId, string sessionId, PlaystateRequest request, CancellationToken cancellationToken);
}
