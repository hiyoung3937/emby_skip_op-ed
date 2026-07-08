using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MediaBrowser.Model.Plugins;

namespace Emby.OpEdSkipper;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    [DisplayName("Enable OP/ED Skipper")]
    [Description("Master switch for automatic OP/ED skip decisions.")]
    public bool Enabled { get; set; } = true;

    [DisplayName("Skip openings")]
    [Description("Skip opening, intro, and OP segments after the user has watched them once.")]
    public bool SkipOpenings { get; set; } = true;

    [DisplayName("Skip endings")]
    [Description("Skip ending, outro, and ED segments after the user has watched them once.")]
    public bool SkipEndings { get; set; } = true;

    [DisplayName("Minimum segment duration")]
    [Description("Segments shorter than this many seconds are ignored.")]
    [Range(1, 600)]
    public int MinimumSegmentSeconds { get; set; } = 10;

    [DisplayName("Seen threshold ratio")]
    [Description("A segment is marked watched after playback reaches this fraction of its duration.")]
    [Range(0.1, 1.0)]
    public double SeenThresholdRatio { get; set; } = 0.70;

    [DisplayName("Seek past segment")]
    [Description("Milliseconds to seek past the segment end to avoid landing exactly on the marker boundary.")]
    [Range(0, 5000)]
    public int SeekPastSegmentMilliseconds { get; set; } = 250;
}
