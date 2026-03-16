namespace Jellyfin.Plugin.CuratedHome.Sections;

internal sealed record SectionDefinition(
    string Id,
    string DisplayText,
    string AdditionalData,
    string? Route);
