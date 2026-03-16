using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CuratedHome.Configuration;

/// <summary>
/// Stores the configurable routing and behavior for Velvet Rows.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of items returned per shelf.
    /// </summary>
    public int RowLimit { get; set; } = 24;

    /// <summary>
    /// Gets or sets a value indicating whether the My Media rail should be promoted to the top for all users.
    /// </summary>
    public bool PromoteMyMediaToTop { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether genre shelves should be published on the home screen and explore pages.
    /// </summary>
    public bool EnableGenreShelves { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether low-confidence filename-like titles should be hidden from curated shelves.
    /// </summary>
    public bool HideLowConfidenceTitles { get; set; } = true;

    /// <summary>
    /// Gets or sets the English movie library ids.
    /// </summary>
    public string EnglishMovieLibraryIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the English TV library ids.
    /// </summary>
    public string EnglishTvLibraryIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Malayalam movie library ids.
    /// </summary>
    public string MalayalamMovieLibraryIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Malayalam TV source library ids.
    /// </summary>
    public string MalayalamTvLibraryIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Malayalam TV metadata match terms.
    /// </summary>
    public string MalayalamTvMatchTerms { get; set; } = "malayalam, മലയാളം";
}
