using Jellyfin.Plugin.CuratedHome.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CuratedHome;

/// <summary>
/// Registers Velvet Rows services with Jellyfin.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers plugin services required for Velvet Rows.
    /// </summary>
    /// <param name="serviceCollection">The active service collection.</param>
    /// <param name="applicationHost">The Jellyfin server host.</param>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CuratedSectionResultsProvider>();
        serviceCollection.AddHostedService<HomeSectionRegistrationService>();
    }
}
