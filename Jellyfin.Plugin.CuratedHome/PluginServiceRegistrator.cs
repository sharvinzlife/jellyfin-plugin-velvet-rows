using Jellyfin.Plugin.CuratedHome.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CuratedHome;

public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CuratedSectionResultsProvider>();
        serviceCollection.AddHostedService<HomeSectionRegistrationService>();
    }
}
