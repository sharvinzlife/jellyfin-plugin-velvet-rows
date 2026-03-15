using System.Reflection;
using MediaBrowser.Common.Configuration;
using Jellyfin.Plugin.CuratedHome.Sections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.CuratedHome.Services;

public sealed class HomeSectionRegistrationService : BackgroundService
{
    private readonly ILogger<HomeSectionRegistrationService> _logger;
    private readonly CuratedSectionResultsProvider _resultsProvider;
    private readonly IApplicationPaths _applicationPaths;
    private bool _registered;

    public HomeSectionRegistrationService(
        ILogger<HomeSectionRegistrationService> logger,
        CuratedSectionResultsProvider resultsProvider,
        IApplicationPaths applicationPaths)
    {
        _logger = logger;
        _resultsProvider = resultsProvider;
        _applicationPaths = applicationPaths;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= 30 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            if (TryRegisterSections())
            {
                _registered = true;
                EnsureCuratedSectionsEnabledForExistingUsers();
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

    private void EnsureCuratedSectionsEnabledForExistingUsers()
    {
        try
        {
            var settingsPath = Path.Combine(
                _applicationPaths.PluginConfigurationsPath,
                "Jellyfin.Plugin.HomeScreenSections",
                "ModularHomeSettings.json");

            if (!File.Exists(settingsPath))
            {
                return;
            }

            var settingsArray = JArray.Parse(File.ReadAllText(settingsPath));
            var curatedSectionIds = SectionDefinitions.All
                .Select(x => x.Id)
                .ToArray();

            var changed = false;

            foreach (var entry in settingsArray.OfType<JObject>())
            {
                if (entry["EnabledSections"] is not JArray enabledSections)
                {
                    enabledSections = [];
                    entry["EnabledSections"] = enabledSections;
                    changed = true;
                }

                var staleCuratedSections = enabledSections
                    .Where(x => ((string?)x)?.StartsWith("Curated", StringComparison.Ordinal) == true)
                    .Where(x => !curatedSectionIds.Contains((string?)x ?? string.Empty, StringComparer.Ordinal))
                    .ToArray();

                foreach (var stale in staleCuratedSections)
                {
                    stale.Remove();
                    changed = true;
                }

                foreach (var sectionId in curatedSectionIds)
                {
                    if (enabledSections.Any(x => string.Equals((string?)x, sectionId, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    enabledSections.Add(sectionId);
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            File.WriteAllText(settingsPath, settingsArray.ToString(Formatting.Indented));
            _logger.LogInformation("Enabled Velvet Rows sections for existing Home Screen Sections users");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Velvet Rows sections into Home Screen Sections user settings");
        }
    }
}
