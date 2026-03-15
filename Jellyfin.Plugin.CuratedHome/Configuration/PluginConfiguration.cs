using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CuratedHome.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public int RowLimit { get; set; } = 24;

    public string EnglishMovieLibraryIds { get; set; } = string.Empty;

    public string EnglishTvLibraryIds { get; set; } = string.Empty;

    public string MalayalamMovieLibraryIds { get; set; } = string.Empty;

    public string MalayalamTvLibraryIds { get; set; } = string.Empty;

    public string MalayalamTvMatchTerms { get; set; } = "malayalam, മലയാളം";
}
