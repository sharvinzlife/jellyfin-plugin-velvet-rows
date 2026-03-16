using System.Reflection;
using System.Xml.Linq;
using Jellyfin.Plugin.CuratedHome.Configuration;
using Jellyfin.Plugin.CuratedHome.Explore;
using MediaBrowser.Common.Configuration;
using Jellyfin.Plugin.CuratedHome.Sections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.CuratedHome.Services;

internal sealed class HomeSectionRegistrationService : BackgroundService
{
    private readonly ILogger<HomeSectionRegistrationService> _logger;
    private readonly CuratedSectionResultsProvider _resultsProvider;
    private readonly IApplicationPaths _applicationPaths;
    private readonly IServiceProvider _serviceProvider;
    private bool _registered;
    private bool _pagesRegistered;

    public HomeSectionRegistrationService(
        ILogger<HomeSectionRegistrationService> logger,
        CuratedSectionResultsProvider resultsProvider,
        IApplicationPaths applicationPaths,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _resultsProvider = resultsProvider;
        _applicationPaths = applicationPaths;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= 30 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            if (!_registered && TryRegisterSections())
            {
                _registered = true;
                var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
                var managedSections = SectionDefinitions.GetEnabled(config);
                UpdateHomeScreenSectionsDefaults(managedSections, config.PromoteMyMediaToTop);
                EnsureCuratedSectionsEnabledForExistingUsers(managedSections, config.PromoteMyMediaToTop);
                _logger.LogInformation("Curated Home Sections registered into Home Screen Sections");
            }

            if (!_pagesRegistered && TryRegisterPluginPages())
            {
                _pagesRegistered = true;
                _logger.LogInformation("Velvet Rows explore pages registered into Plugin Pages");
            }

            if (_registered && _pagesRegistered)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        if (!_registered && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Home Screen Sections was not available; curated rows were not registered");
        }

        if (!_pagesRegistered && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Plugin Pages was not available; Velvet Rows explore pages were not registered");
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

        foreach (var definition in SectionDefinitions.GetEnabled(Plugin.Instance?.Configuration ?? new PluginConfiguration()))
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

    private bool TryRegisterPluginPages()
    {
        var pluginPagesAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(x => x.GetName().Name == "Jellyfin.Plugin.PluginPages");

        if (pluginPagesAssembly is null)
        {
            return false;
        }

        var managerType = pluginPagesAssembly.GetType("Jellyfin.Plugin.PluginPages.Library.IPluginPagesManager");
        var pageType = pluginPagesAssembly.GetType("Jellyfin.Plugin.PluginPages.Library.PluginPage");
        if (managerType is null || pageType is null)
        {
            return false;
        }

        var manager = _serviceProvider.GetService(managerType);
        var registerMethod = managerType.GetMethod("RegisterPluginPage", BindingFlags.Public | BindingFlags.Instance);
        if (manager is null || registerMethod is null)
        {
            return false;
        }

        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        foreach (var definition in ExplorePageCatalog.GetAll(config))
        {
            var page = Activator.CreateInstance(pageType);
            if (page is null)
            {
                continue;
            }

            pageType.GetProperty("Id")?.SetValue(page, $"velvet-rows-{definition.Key}");
            pageType.GetProperty("Url")?.SetValue(page, $"/VelvetRows/Page?page={definition.Key}");
            pageType.GetProperty("DisplayText")?.SetValue(page, definition.MenuText);
            pageType.GetProperty("Icon")?.SetValue(page, definition.Icon);
            registerMethod.Invoke(manager, [page]);
        }

        return true;
    }

    private void UpdateHomeScreenSectionsDefaults(IReadOnlyList<SectionDefinition> managedSections, bool promoteMyMediaToTop)
    {
        try
        {
            var homeSectionsAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => x.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");

            if (homeSectionsAssembly is null)
            {
                return;
            }

            var pluginType = homeSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.HomeScreenSectionsPlugin");
            var pluginInstance = pluginType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var configuration = pluginType?.GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance)?.GetValue(pluginInstance);
            var updateMethod = pluginType?.GetMethod("UpdateConfiguration", BindingFlags.Public | BindingFlags.Instance);
            if (pluginType is null || pluginInstance is null || configuration is null || updateMethod is null)
            {
                return;
            }

            var configurationType = configuration.GetType();
            var sectionSettingsProperty = configurationType.GetProperty("SectionSettings", BindingFlags.Public | BindingFlags.Instance);
            var enabledProperty = configurationType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
            if (sectionSettingsProperty is null || enabledProperty is null)
            {
                return;
            }

            var sectionSettingsType = homeSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.Configuration.SectionSettings");
            if (sectionSettingsType is null)
            {
                return;
            }

            var currentSettings = (sectionSettingsProperty.GetValue(configuration) as System.Collections.IEnumerable)?
                .Cast<object>()
                .ToList() ?? new List<object>();

            var curatedIds = managedSections.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            currentSettings = currentSettings
                .Where(x =>
                {
                    var id = sectionSettingsType.GetProperty("SectionId")?.GetValue(x) as string ?? string.Empty;
                    return !id.StartsWith("Curated", StringComparison.Ordinal) || curatedIds.Contains(id);
                })
                .ToList();

            if (promoteMyMediaToTop)
            {
                currentSettings.RemoveAll(x => string.Equals(sectionSettingsType.GetProperty("SectionId")?.GetValue(x) as string, "MyMedia", StringComparison.Ordinal));
                currentSettings.Add(CreateSectionSetting(sectionSettingsType, "MyMedia", 0, true, false));
            }

            var orderIndex = 30;
            foreach (var section in managedSections)
            {
                currentSettings.RemoveAll(x => string.Equals(sectionSettingsType.GetProperty("SectionId")?.GetValue(x) as string, section.Id, StringComparison.Ordinal));
                currentSettings.Add(CreateSectionSetting(sectionSettingsType, section.Id, orderIndex, true, false));
                orderIndex += 10;
            }

            currentSettings = currentSettings
                .OrderBy(x => Convert.ToInt32(sectionSettingsType.GetProperty("OrderIndex")?.GetValue(x) ?? 9999))
                .ToList();

            var typedArray = Array.CreateInstance(sectionSettingsType, currentSettings.Count);
            for (var i = 0; i < currentSettings.Count; i++)
            {
                typedArray.SetValue(currentSettings[i], i);
            }

            enabledProperty.SetValue(configuration, true);
            sectionSettingsProperty.SetValue(configuration, typedArray);
            updateMethod.Invoke(pluginInstance, [configuration]);
            PersistHomeScreenSectionsConfigFile(managedSections, promoteMyMediaToTop);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize Velvet Rows ordering into Home Screen Sections configuration");
        }
    }

    private static object CreateSectionSetting(Type sectionSettingsType, string sectionId, int orderIndex, bool enabled, bool allowUserOverride)
    {
        var instance = Activator.CreateInstance(sectionSettingsType)
            ?? throw new InvalidOperationException("Unable to create Home Screen Sections setting instance.");
        sectionSettingsType.GetProperty("SectionId")?.SetValue(instance, sectionId);
        sectionSettingsType.GetProperty("Enabled")?.SetValue(instance, enabled);
        sectionSettingsType.GetProperty("AllowUserOverride")?.SetValue(instance, allowUserOverride);
        sectionSettingsType.GetProperty("LowerLimit")?.SetValue(instance, 0);
        sectionSettingsType.GetProperty("UpperLimit")?.SetValue(instance, 1);
        sectionSettingsType.GetProperty("OrderIndex")?.SetValue(instance, orderIndex);
        var viewModeType = sectionSettingsType.GetProperty("ViewMode")?.PropertyType;
        if (viewModeType is not null)
        {
            var landscape = Enum.Parse(viewModeType, "Landscape");
            sectionSettingsType.GetProperty("ViewMode")?.SetValue(instance, landscape);
        }

        sectionSettingsType.GetProperty("HideWatchedItems")?.SetValue(instance, false);
        return instance;
    }

    private void PersistHomeScreenSectionsConfigFile(IReadOnlyList<SectionDefinition> managedSections, bool promoteMyMediaToTop)
    {
        var configPath = Path.Combine(
            _applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.HomeScreenSections.xml");

        if (!File.Exists(configPath))
        {
            return;
        }

        var document = XDocument.Load(configPath);
        var root = document.Root;
        if (root is null)
        {
            return;
        }

        var sectionSettingsRoot = root.Element("SectionSettings");
        if (sectionSettingsRoot is null)
        {
            sectionSettingsRoot = new XElement("SectionSettings");
            root.Add(sectionSettingsRoot);
        }

        var managedIds = managedSections.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var stale in sectionSettingsRoot.Elements("SectionSettings")
                     .Where(x => (string?)x.Element("SectionId") is string id && (id == "MyMedia" || id.StartsWith("Curated", StringComparison.Ordinal)) && !managedIds.Contains(id) && !string.Equals(id, "MyMedia", StringComparison.Ordinal))
                     .ToList())
        {
            stale.Remove();
        }

        if (promoteMyMediaToTop)
        {
            EnsureSectionSettingXml(sectionSettingsRoot, "MyMedia", 0, true, false);
        }

        var orderIndex = 30;
        foreach (var section in managedSections)
        {
            EnsureSectionSettingXml(sectionSettingsRoot, section.Id, orderIndex, true, false);
            orderIndex += 10;
        }

        document.Save(configPath);
    }

    private static void EnsureSectionSettingXml(XElement sectionSettingsRoot, string sectionId, int orderIndex, bool enabled, bool allowUserOverride)
    {
        var sectionElement = sectionSettingsRoot.Elements("SectionSettings")
            .FirstOrDefault(x => string.Equals((string?)x.Element("SectionId"), sectionId, StringComparison.Ordinal));

        if (sectionElement is null)
        {
            sectionElement = new XElement("SectionSettings");
            sectionSettingsRoot.Add(sectionElement);
        }

        SetElementValue(sectionElement, "SectionId", sectionId);
        SetElementValue(sectionElement, "Enabled", enabled.ToString());
        SetElementValue(sectionElement, "AllowUserOverride", allowUserOverride.ToString());
        SetElementValue(sectionElement, "LowerLimit", "0");
        SetElementValue(sectionElement, "UpperLimit", "1");
        SetElementValue(sectionElement, "OrderIndex", orderIndex.ToString());
        SetElementValue(sectionElement, "ViewMode", "Landscape");
        SetElementValue(sectionElement, "HideWatchedItems", "false");
    }

    private static void SetElementValue(XElement parent, string name, string value)
    {
        var element = parent.Element(name);
        if (element is null)
        {
            parent.Add(new XElement(name, value));
            return;
        }

        element.Value = value;
    }

    private void EnsureCuratedSectionsEnabledForExistingUsers(IReadOnlyList<SectionDefinition> managedSections, bool promoteMyMediaToTop)
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
            var curatedSectionIds = managedSections
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

                if (promoteMyMediaToTop)
                {
                    var nonManaged = enabledSections
                        .Select(x => (string?)x)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Where(x => !string.Equals(x, "MyMedia", StringComparison.Ordinal))
                        .Where(x => !curatedSectionIds.Contains(x!, StringComparer.Ordinal))
                        .Cast<string>()
                        .ToArray();

                    enabledSections.RemoveAll();
                    enabledSections.Add("MyMedia");
                    foreach (var sectionId in nonManaged)
                    {
                        enabledSections.Add(sectionId);
                    }

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
