using Jellyfin.Plugin.CuratedHome.Configuration;

namespace Jellyfin.Plugin.CuratedHome.Sections;

internal static class SectionDefinitions
{
    private const string FeaturedMoviesPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Dfeatured-movies";
    private const string FeaturedShowsPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Dfeatured-tv";
    private const string EnglishMoviesPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Denglish-movies";
    private const string EnglishShowsPageRoute = "userpluginsettings.html?pageUrl=%2FVelvetRows%2FPage%3Fpage%3Denglish-tv";

    public static IReadOnlyList<SectionDefinition> GetEnabled(PluginConfiguration config)
    {
        var focusedLanguage = GetFocusedLanguageDisplayName(config);

        var core = new SectionDefinition[]
        {
            new("CuratedFeaturedMoviesRecentlyAdded", $"{focusedLanguage} Movies - Newly Added", "featured_movies_recent", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesLatest", $"{focusedLanguage} Movies - Newly Released", "featured_movies_latest", FeaturedMoviesPageRoute),
            new("CuratedFeaturedShowsRecentlyAdded", $"{focusedLanguage} TV Shows - Newly Added", "featured_shows_recent", FeaturedShowsPageRoute),
            new("CuratedFeaturedShowsLatest", $"{focusedLanguage} TV Shows - Newly Released", "featured_shows_latest", FeaturedShowsPageRoute),
            new("CuratedEnglishMoviesRecentlyAdded", "English Movies - Newly Added", "english_movies_recent", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesLatest", "English Movies - Newly Released", "english_movies_latest", EnglishMoviesPageRoute),
            new("CuratedEnglishShowsRecentlyAdded", "English TV Shows - Newly Added", "english_shows_recent", EnglishShowsPageRoute),
            new("CuratedEnglishShowsLatest", "English TV Shows - Newly Released", "english_shows_latest", EnglishShowsPageRoute),
        };

        if (!config.EnableGenreShelves)
        {
            return core;
        }

        var genres = new SectionDefinition[]
        {
            new("CuratedFeaturedMoviesRomance", $"{focusedLanguage} Movies - Romance and Love", "featured_movies_romance", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesThriller", $"{focusedLanguage} Movies - Thriller and Suspense", "featured_movies_thriller", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesAction", $"{focusedLanguage} Movies - Action and Adventure", "featured_movies_action", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesComedy", $"{focusedLanguage} Movies - Comedy", "featured_movies_comedy", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesCrime", $"{focusedLanguage} Movies - Crime and Mystery", "featured_movies_crime", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesFamily", $"{focusedLanguage} Movies - Family", "featured_movies_family", FeaturedMoviesPageRoute),
            new("CuratedFeaturedMoviesMystery", $"{focusedLanguage} Movies - Mystery", "featured_movies_mystery", FeaturedMoviesPageRoute),
            new("CuratedEnglishMoviesRomance", "English Movies - Romance and Love", "english_movies_romance", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesThriller", "English Movies - Thriller and Suspense", "english_movies_thriller", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesAction", "English Movies - Action and Adventure", "english_movies_action", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesComedy", "English Movies - Comedy", "english_movies_comedy", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesCrime", "English Movies - Crime and Mystery", "english_movies_crime", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesFamily", "English Movies - Family", "english_movies_family", EnglishMoviesPageRoute),
            new("CuratedEnglishMoviesMystery", "English Movies - Mystery", "english_movies_mystery", EnglishMoviesPageRoute),
        };

        return core.Concat(genres).ToArray();
    }

    private static string GetFocusedLanguageDisplayName(PluginConfiguration config)
    {
        return string.IsNullOrWhiteSpace(config.FocusedLanguageDisplayName)
            ? "Focused Language"
            : config.FocusedLanguageDisplayName.Trim();
    }
}
