using System.Reflection;
using Jellyfin.Plugin.CuratedHome.Sections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.CuratedHome.Services;

public sealed class HomeSectionRegistrationService : BackgroundService
{
    private readonly ILogger<HomeSectionRegistrationService> _logger;
    private readonly CuratedSectionResultsProvider _resultsProvider;
    private bool _registered;

    public HomeSectionRegistrationService(
        ILogger<HomeSectionRegistrationService> logger,
        CuratedSectionResultsProvider resultsProvider)
    {
        _logger = logger;
        _resultsProvider = resultsProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= 30 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            if (TryRegisterSections())
            {
                _registered = true;
                _logger.LogInformation("Curated Home Sections registered into Home Screen Sections");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        if (!_registered && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Home Screen Sections was not available; curated rows were not registered");
        }
    }

    private bool TryRegisterSections()
    {
        var homeSectionsAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(x => x.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");

        if (homeSectionsAssembly is null)
        {
            return false;
        }

        var pluginInterfaceType = homeSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
        var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);
        if (registerMethod is null)
        {
            return false;
        }

        var providerType = _resultsProvider.GetType();
        var providerAssemblyName = providerType.Assembly.FullName;
        var providerTypeName = providerType.FullName;
        if (string.IsNullOrWhiteSpace(providerAssemblyName) || string.IsNullOrWhiteSpace(providerTypeName))
        {
            return false;
        }

        foreach (var definition in SectionDefinitions.All)
        {
            var payload = JObject.FromObject(new
            {
                id = definition.Id,
                displayText = definition.DisplayText,
                route = definition.Route,
                additionalData = definition.AdditionalData,
                resultsAssembly = providerAssemblyName,
                resultsClass = providerTypeName,
                resultsMethod = nameof(CuratedSectionResultsProvider.GetResults),
            });

            registerMethod.Invoke(null, [payload]);
        }

        return true;
    }
}
