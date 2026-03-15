namespace Jellyfin.Plugin.CuratedHome.Sections;

public static class SectionDefinitions
{
    public static IReadOnlyList<SectionDefinition> All { get; } =
    [
        new("CuratedMalayalamMoviesRecentlyAdded", "Malayalam Movies - Newly Added", "malayalam_movies_recent", "movies"),
        new("CuratedMalayalamMoviesLatest", "Malayalam Movies - Newly Released", "malayalam_movies_latest", "movies"),
        new("CuratedMalayalamShowsRecentlyAdded", "Malayalam TV Shows - Newly Added", "malayalam_shows_recent", "tvshows"),
        new("CuratedMalayalamShowsLatest", "Malayalam TV Shows - Newly Released", "malayalam_shows_latest", "tvshows"),
        new("CuratedEnglishMoviesRecentlyAdded", "English Movies - Newly Added", "english_movies_recent", "movies"),
        new("CuratedEnglishMoviesLatest", "English Movies - Newly Released", "english_movies_latest", "movies"),
        new("CuratedTamilMoviesRecentlyAdded", "Tamil Movies - Newly Added", "tamil_movies_recent", "movies"),
        new("CuratedTamilMoviesLatest", "Tamil Movies - Newly Released", "tamil_movies_latest", "movies"),
    ];
}
