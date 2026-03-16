using Jellyfin.Plugin.CuratedHome.Configuration;

namespace Jellyfin.Plugin.CuratedHome.Sections;

internal static class SectionDefinitions
{
    private const string MalayalamMoviesPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Dmalayalam-movies";
    private const string MalayalamShowsPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Dmalayalam-tv";
    private const string EnglishMoviesPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Denglish-movies";
    private const string EnglishShowsPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Denglish-tv";

    private static IReadOnlyList<SectionDefinition> Core { get; } =
    [
        new("CuratedMalayalamMoviesRecentlyAdded", "Malayalam Movies - Newly Added", "malayalam_movies_recent", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesLatest", "Malayalam Movies - Newly Released", "malayalam_movies_latest", MalayalamMoviesPageRoute),
        new("CuratedMalayalamShowsRecentlyAdded", "Malayalam TV Shows - Newly Added", "malayalam_shows_recent", MalayalamShowsPageRoute),
        new("CuratedMalayalamShowsLatest", "Malayalam TV Shows - Newly Released", "malayalam_shows_latest", MalayalamShowsPageRoute),
        new("CuratedEnglishMoviesRecentlyAdded", "English Movies - Newly Added", "english_movies_recent", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesLatest", "English Movies - Newly Released", "english_movies_latest", EnglishMoviesPageRoute),
        new("CuratedEnglishShowsRecentlyAdded", "English TV Shows - Newly Added", "english_shows_recent", EnglishShowsPageRoute),
        new("CuratedEnglishShowsLatest", "English TV Shows - Newly Released", "english_shows_latest", EnglishShowsPageRoute),
    ];

    private static IReadOnlyList<SectionDefinition> GenreShelves { get; } =
    [
        new("CuratedMalayalamMoviesRomance", "Malayalam Movies - Romance and Love", "malayalam_movies_romance", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesThriller", "Malayalam Movies - Thriller and Suspense", "malayalam_movies_thriller", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesAction", "Malayalam Movies - Action and Adventure", "malayalam_movies_action", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesComedy", "Malayalam Movies - Comedy", "malayalam_movies_comedy", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesCrime", "Malayalam Movies - Crime and Mystery", "malayalam_movies_crime", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesFamily", "Malayalam Movies - Family", "malayalam_movies_family", MalayalamMoviesPageRoute),
        new("CuratedMalayalamMoviesMystery", "Malayalam Movies - Mystery", "malayalam_movies_mystery", MalayalamMoviesPageRoute),
        new("CuratedEnglishMoviesRomance", "English Movies - Romance and Love", "english_movies_romance", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesThriller", "English Movies - Thriller and Suspense", "english_movies_thriller", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesAction", "English Movies - Action and Adventure", "english_movies_action", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesComedy", "English Movies - Comedy", "english_movies_comedy", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesCrime", "English Movies - Crime and Mystery", "english_movies_crime", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesFamily", "English Movies - Family", "english_movies_family", EnglishMoviesPageRoute),
        new("CuratedEnglishMoviesMystery", "English Movies - Mystery", "english_movies_mystery", EnglishMoviesPageRoute),
    ];

    public static IReadOnlyList<SectionDefinition> GetEnabled(PluginConfiguration config)
    {
        if (config.EnableGenreShelves)
        {
            return Core.Concat(GenreShelves).ToArray();
        }

        return Core;
    }
}
