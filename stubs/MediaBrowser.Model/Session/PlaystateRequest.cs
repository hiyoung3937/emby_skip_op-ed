namespace MediaBrowser.Model.Session;

public sealed class PlaystateRequest
{
    public PlaystateCommand Command { get; set; }

    public long? SeekPositionTicks { get; set; }
}
