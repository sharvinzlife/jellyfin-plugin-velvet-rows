namespace Jellyfin.Plugin.CuratedHome.Sections;

public sealed record SectionDefinition(
    string Id,
    string DisplayText,
    string AdditionalData,
    string? Route);
