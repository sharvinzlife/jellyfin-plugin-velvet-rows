using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.CuratedHome.Configuration;

namespace Jellyfin.Plugin.CuratedHome;

/// <summary>
/// Jellyfin plugin entry point for Velvet Rows.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the active Velvet Rows plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The Jellyfin application paths service.</param>
    /// <param name="xmlSerializer">The XML serializer used for plugin configuration.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Velvet Rows";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("4d9b1d2f-0a66-4d5f-9aa3-2a0a7ce4c7ab");

    /// <inheritdoc />
    public override string Description => "Curated Jellyfin home rows for language-aware newly added and newly released media.";

    /// <summary>
    /// Returns the admin configuration pages embedded in this plugin.
    /// </summary>
    /// <returns>The plugin configuration pages.</returns>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}
