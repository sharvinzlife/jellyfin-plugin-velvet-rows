using Jellyfin.Plugin.CuratedHome.Configuration;

namespace Jellyfin.Plugin.CuratedHome.Explore;

internal static class ExplorePageCatalog
{
    public static IReadOnlyList<ExplorePageDefinition> GetAll(PluginConfiguration config)
    {
        var focusedLanguage = GetFocusedLanguageDisplayName(config);

        return
        [
            new(
                "featured-movies",
                $"{focusedLanguage} Movies",
                $"{focusedLanguage} Movies Explore",
                "movie",
                $"Curated {focusedLanguage} movie shelves for new drops, trusted releases, and mood-driven discovery.",
                BuildMovieShelves("featured_movies", focusedLanguage)),
            new(
                "english-movies",
                "English Movies",
                "English Movies Explore",
                "movie",
                "Curated English movie shelves for recent arrivals, trusted releases, and genre-led browsing.",
                BuildMovieShelves("english_movies", "English")),
            new(
                "featured-tv",
                $"{focusedLanguage} TV Shows",
                $"{focusedLanguage} TV Explore",
                "live_tv",
                $"Curated {focusedLanguage} TV shelves for new episodes, recent premieres, and genre-led discovery.",
                BuildShowShelves("featured_shows", focusedLanguage)),
            new(
                "english-tv",
                "English TV Shows",
                "English TV Explore",
                "live_tv",
                "Curated English TV shelves for new episodes, recent premieres, and genre-led discovery.",
                BuildShowShelves("english_shows", "English")),
        ];
    }

    public static ExplorePageDefinition? Find(string key, PluginConfiguration config)
    {
        return GetAll(config).FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ExploreShelfDefinition> BuildMovieShelves(string prefix, string language)
    {
        return
        [
            new ExploreShelfDefinition($"{prefix}_recent", "Newly Added", $"Fresh arrivals from your {language} movie library."),
            new ExploreShelfDefinition($"{prefix}_latest", "Newly Released", "Recent releases sorted with trusted release metadata."),
            new ExploreShelfDefinition($"{prefix}_romance", "Romance and Love", $"Romantic {language} films with cleaner metadata."),
            new ExploreShelfDefinition($"{prefix}_thriller", "Thriller and Suspense", $"Mystery and suspense-driven {language} movie picks."),
            new ExploreShelfDefinition($"{prefix}_action", "Action and Adventure", $"Action-led {language} movies ready for a bigger screen."),
            new ExploreShelfDefinition($"{prefix}_comedy", "Comedy", $"{language} comedies and lighter crowd-pleasers."),
            new ExploreShelfDefinition($"{prefix}_crime", "Crime and Mystery", $"Crime, investigation, and darker {language} mystery picks."),
            new ExploreShelfDefinition($"{prefix}_family", "Family", $"Warm {language} family films with cleaner metadata."),
            new ExploreShelfDefinition($"{prefix}_mystery", "Mystery", $"{language} mystery-first titles worth exploring."),
        ];
    }

    private static IReadOnlyList<ExploreShelfDefinition> BuildShowShelves(string prefix, string language)
    {
        return
        [
            new ExploreShelfDefinition($"{prefix}_recent", "Newly Added", $"Shows with the freshest {language} episode activity."),
            new ExploreShelfDefinition($"{prefix}_latest", "Newly Released", "Shows ordered by trusted series premiere metadata."),
            new ExploreShelfDefinition($"{prefix}_romance", "Romance and Love", $"{language} series with romance-forward metadata."),
            new ExploreShelfDefinition($"{prefix}_thriller", "Thriller and Suspense", $"{language} thrillers, mysteries, and tense drama series."),
            new ExploreShelfDefinition($"{prefix}_action", "Action and Adventure", $"{language} shows with action and adventure metadata."),
            new ExploreShelfDefinition($"{prefix}_comedy", "Comedy", $"{language} comedy-led series and lighter watches."),
            new ExploreShelfDefinition($"{prefix}_crime", "Crime and Mystery", $"{language} crime and investigation series."),
            new ExploreShelfDefinition($"{prefix}_family", "Family", $"{language} family dramas and gentler series."),
            new ExploreShelfDefinition($"{prefix}_mystery", "Mystery", $"{language} mystery-led series with strong metadata."),
        ];
    }

    private static string GetFocusedLanguageDisplayName(PluginConfiguration config)
    {
        return string.IsNullOrWhiteSpace(config.FocusedLanguageDisplayName)
            ? "Focused Language"
            : config.FocusedLanguageDisplayName.Trim();
    }
}

internal sealed record ExplorePageDefinition(
    string Key,
    string Title,
    string MenuText,
    string Icon,
    string Description,
    IReadOnlyList<ExploreShelfDefinition> Shelves);

internal sealed record ExploreShelfDefinition(
    string DataKey,
    string Title,
    string Description);
