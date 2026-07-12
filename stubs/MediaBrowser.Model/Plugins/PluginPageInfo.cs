namespace MediaBrowser.Model.Plugins;

public sealed class PluginPageInfo
{
    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? EmbeddedResourcePath { get; set; }

    public bool EnableInMainMenu { get; set; }

    public bool EnableInUserMenu { get; set; }

    public string? MenuSection { get; set; }

    public string? FeatureId { get; set; }

    public string? MenuIcon { get; set; }

    public bool IsMainConfigPage { get; set; }
}
