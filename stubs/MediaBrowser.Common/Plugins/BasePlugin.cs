using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Common.Plugins;

public abstract class BasePlugin<TConfiguration>
    where TConfiguration : BasePluginConfiguration, new()
{
    protected BasePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
    {
        Configuration = new TConfiguration();
    }

    public TConfiguration Configuration { get; }

    public virtual string Name => GetType().Name;

    public virtual string Description => string.Empty;

    public virtual Guid Id => Guid.Empty;

    protected string DataFolderPath => AppContext.BaseDirectory;
}
