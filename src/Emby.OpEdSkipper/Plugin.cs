using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.OpEdSkipper;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginId = Guid.Parse("4f9b03b2-3fa2-4b3f-8f27-cd5126a5f3cc");

    public static Plugin? Instance { get; private set; }

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "OP/ED Skipper";

    public override string Description => "Skips watched opening and ending segments after the first full watch.";

    public override Guid Id => PluginId;

    public string StateFilePath => Path.Combine(DataFolderPath, "seen-segments.json");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "opedskipper",
                DisplayName = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                EnableInMainMenu = false,
                IsMainConfigPage = true
            }
        };
    }
}
