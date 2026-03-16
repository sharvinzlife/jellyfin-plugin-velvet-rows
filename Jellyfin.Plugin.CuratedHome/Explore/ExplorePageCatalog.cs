namespace Jellyfin.Plugin.CuratedHome.Explore;

internal static class ExplorePageCatalog
{
    public static IReadOnlyList<ExplorePageDefinition> All { get; } =
    [
        new(
            "malayalam-movies",
            "Malayalam Movies",
            "Malayalam Movies Explore",
            "movie",
            "Curated Malayalam movie shelves for new drops, trusted releases, and mood-driven discovery.",
            [
                new ExploreShelfDefinition("malayalam_movies_recent", "Newly Added", "Fresh arrivals from your Malayalam movie library."),
                new ExploreShelfDefinition("malayalam_movies_latest", "Newly Released", "Recent releases sorted with trusted release metadata."),
                new ExploreShelfDefinition("malayalam_movies_romance", "Romance and Love", "Romantic Malayalam films with cleaner metadata."),
                new ExploreShelfDefinition("malayalam_movies_thriller", "Thriller and Suspense", "Mystery and suspense-driven Malayalam movie picks."),
                new ExploreShelfDefinition("malayalam_movies_action", "Action and Adventure", "Action-led Malayalam movies ready for a bigger screen."),
                new ExploreShelfDefinition("malayalam_movies_comedy", "Comedy", "Malayalam comedies and lighter crowd-pleasers."),
                new ExploreShelfDefinition("malayalam_movies_crime", "Crime and Mystery", "Crime, investigation, and darker mystery picks."),
                new ExploreShelfDefinition("malayalam_movies_family", "Family", "Warm Malayalam family films with cleaner metadata."),
                new ExploreShelfDefinition("malayalam_movies_mystery", "Mystery", "Malayalam mystery-first titles worth exploring."),
            ]),
        new(
            "english-movies",
            "English Movies",
            "English Movies Explore",
            "movie",
            "Curated English movie shelves for recent arrivals, trusted releases, and genre-led browsing.",
            [
                new ExploreShelfDefinition("english_movies_recent", "Newly Added", "Fresh arrivals from your English movie library."),
                new ExploreShelfDefinition("english_movies_latest", "Newly Released", "Recent releases sorted with trusted release metadata."),
                new ExploreShelfDefinition("english_movies_romance", "Romance and Love", "Romantic English movies with clean metadata."),
                new ExploreShelfDefinition("english_movies_thriller", "Thriller and Suspense", "Thrillers, mysteries, and tense English movie picks."),
                new ExploreShelfDefinition("english_movies_action", "Action and Adventure", "Action-heavy and adventure-led English movie picks."),
                new ExploreShelfDefinition("english_movies_comedy", "Comedy", "English comedies and easy rewatch picks."),
                new ExploreShelfDefinition("english_movies_crime", "Crime and Mystery", "Crime and mystery-led English movie picks."),
                new ExploreShelfDefinition("english_movies_family", "Family", "Family-friendly English movies with cleaner metadata."),
                new ExploreShelfDefinition("english_movies_mystery", "Mystery", "Mystery-led English movies with strong metadata."),
            ]),
        new(
            "malayalam-tv",
            "Malayalam TV Shows",
            "Malayalam TV Explore",
            "live_tv",
            "Curated Malayalam TV shelves for new episodes, recent premieres, and genre-led discovery.",
            [
                new ExploreShelfDefinition("malayalam_shows_recent", "Newly Added", "Shows with the freshest Malayalam episode activity."),
                new ExploreShelfDefinition("malayalam_shows_latest", "Newly Released", "Shows ordered by trusted series premiere metadata."),
                new ExploreShelfDefinition("malayalam_shows_romance", "Romance and Love", "Malayalam series with romance-forward metadata."),
                new ExploreShelfDefinition("malayalam_shows_thriller", "Thriller and Suspense", "Malayalam thrillers, mysteries, and tense drama series."),
                new ExploreShelfDefinition("malayalam_shows_action", "Action and Adventure", "Malayalam shows with action and adventure metadata."),
                new ExploreShelfDefinition("malayalam_shows_comedy", "Comedy", "Malayalam comedy-led series and lighter watches."),
                new ExploreShelfDefinition("malayalam_shows_crime", "Crime and Mystery", "Malayalam crime and investigation series."),
                new ExploreShelfDefinition("malayalam_shows_family", "Family", "Malayalam family dramas and gentler series."),
                new ExploreShelfDefinition("malayalam_shows_mystery", "Mystery", "Malayalam mystery-led series with strong metadata."),
            ]),
        new(
            "english-tv",
            "English TV Shows",
            "English TV Explore",
            "live_tv",
            "Curated English TV shelves for new episodes, recent premieres, and genre-led discovery.",
            [
                new ExploreShelfDefinition("english_shows_recent", "Newly Added", "Shows with the freshest English episode activity."),
                new ExploreShelfDefinition("english_shows_latest", "Newly Released", "Shows ordered by trusted series premiere metadata."),
                new ExploreShelfDefinition("english_shows_romance", "Romance and Love", "English series with romance-forward metadata."),
                new ExploreShelfDefinition("english_shows_thriller", "Thriller and Suspense", "English thrillers, mysteries, and suspense picks."),
                new ExploreShelfDefinition("english_shows_action", "Action and Adventure", "English shows with action and adventure metadata."),
                new ExploreShelfDefinition("english_shows_comedy", "Comedy", "English comedy series and comfort rewatches."),
                new ExploreShelfDefinition("english_shows_crime", "Crime and Mystery", "English crime, detective, and mystery series."),
                new ExploreShelfDefinition("english_shows_family", "Family", "English family-friendly series and softer drama picks."),
                new ExploreShelfDefinition("english_shows_mystery", "Mystery", "Mystery-first English series with clean metadata."),
            ]),
    ];

    public static ExplorePageDefinition? Find(string key)
    {
        return All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
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
