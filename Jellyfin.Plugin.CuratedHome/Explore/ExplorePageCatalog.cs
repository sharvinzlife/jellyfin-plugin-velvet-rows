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
            "Curated Malayalam movie shelves built around rotating spotlight mixes, explicit latest lanes, recent additions, and mood-driven discovery.",
            [
                new ExploreShelfDefinition("malayalam_movies_recent", "Spotlight Mix", "A high-rotation blend of fresh titles, library staples, and underplayed Malayalam movie picks."),
                new ExploreShelfDefinition("malayalam_movies_latest", "Wildcard Rotation", "A wider Malayalam movie swing that intentionally mixes new drops, deep cuts, and strong metadata matches."),
                new ExploreShelfDefinition("malayalam_movies_recently_added", "Recently Added", "Strict newest-by-library-addition Malayalam movie picks for when you want the freshest arrivals."),
                new ExploreShelfDefinition("malayalam_movies_latest_releases", "Latest", "Strict newest-by-release Malayalam movie picks with clean metadata."),
                new ExploreShelfDefinition("malayalam_movies_romance", "Romance and Love", "Romantic Malayalam films with cleaner metadata.", true),
                new ExploreShelfDefinition("malayalam_movies_thriller", "Thriller and Suspense", "Mystery and suspense-driven Malayalam movie picks.", true),
                new ExploreShelfDefinition("malayalam_movies_action", "Action and Adventure", "Action-led Malayalam movies ready for a bigger screen.", true),
                new ExploreShelfDefinition("malayalam_movies_comedy", "Comedy", "Malayalam comedies and lighter crowd-pleasers.", true),
                new ExploreShelfDefinition("malayalam_movies_crime", "Crime and Mystery", "Crime, investigation, and darker mystery picks.", true),
                new ExploreShelfDefinition("malayalam_movies_family", "Family", "Warm Malayalam family films with cleaner metadata.", true),
                new ExploreShelfDefinition("malayalam_movies_mystery", "Mystery", "Malayalam mystery-first titles worth exploring.", true),
            ]),
        new(
            "english-movies",
            "English Movies",
            "English Movies Explore",
            "movie",
            "Curated English movie shelves built around rotating spotlight mixes, explicit latest lanes, recent additions, and genre-led browsing.",
            [
                new ExploreShelfDefinition("english_movies_recent", "Spotlight Mix", "A high-rotation blend of fresh titles, library staples, and underplayed English movie picks."),
                new ExploreShelfDefinition("english_movies_latest", "Wildcard Rotation", "A wider English movie swing that intentionally mixes new drops, deep cuts, and strong metadata matches."),
                new ExploreShelfDefinition("english_movies_recently_added", "Recently Added", "Strict newest-by-library-addition English movie picks for when you want the freshest arrivals."),
                new ExploreShelfDefinition("english_movies_latest_releases", "Latest", "Strict newest-by-release English movie picks with clean metadata."),
                new ExploreShelfDefinition("english_movies_romance", "Romance and Love", "Romantic English movies with clean metadata.", true),
                new ExploreShelfDefinition("english_movies_thriller", "Thriller and Suspense", "Thrillers, mysteries, and tense English movie picks.", true),
                new ExploreShelfDefinition("english_movies_action", "Action and Adventure", "Action-heavy and adventure-led English movie picks.", true),
                new ExploreShelfDefinition("english_movies_comedy", "Comedy", "English comedies and easy rewatch picks.", true),
                new ExploreShelfDefinition("english_movies_crime", "Crime and Mystery", "Crime and mystery-led English movie picks.", true),
                new ExploreShelfDefinition("english_movies_family", "Family", "Family-friendly English movies with cleaner metadata.", true),
                new ExploreShelfDefinition("english_movies_mystery", "Mystery", "Mystery-led English movies with strong metadata.", true),
            ]),
        new(
            "malayalam-tv",
            "Malayalam TV Shows",
            "Malayalam TV Explore",
            "live_tv",
            "Curated Malayalam TV shelves built around rotating spotlight mixes, explicit latest lanes, recent additions, and genre-led discovery.",
            [
                new ExploreShelfDefinition("malayalam_shows_recent", "Spotlight Mix", "A high-rotation blend of fresh, beloved, and underplayed Malayalam shows."),
                new ExploreShelfDefinition("malayalam_shows_latest", "Wildcard Rotation", "A broader Malayalam TV swing that mixes newer series, older favorites, and surprise pulls."),
                new ExploreShelfDefinition("malayalam_shows_recently_added", "Recently Added", "Strict newest-by-library-addition Malayalam series for when you want the freshest arrivals."),
                new ExploreShelfDefinition("malayalam_shows_latest_releases", "Latest", "Strict newest-by-release Malayalam series with cleaner metadata."),
                new ExploreShelfDefinition("malayalam_shows_romance", "Romance and Love", "Malayalam series with romance-forward metadata.", true),
                new ExploreShelfDefinition("malayalam_shows_thriller", "Thriller and Suspense", "Malayalam thrillers, mysteries, and tense drama series.", true),
                new ExploreShelfDefinition("malayalam_shows_action", "Action and Adventure", "Malayalam shows with action and adventure metadata.", true),
                new ExploreShelfDefinition("malayalam_shows_comedy", "Comedy", "Malayalam comedy-led series and lighter watches.", true),
                new ExploreShelfDefinition("malayalam_shows_crime", "Crime and Mystery", "Malayalam crime and investigation series.", true),
                new ExploreShelfDefinition("malayalam_shows_family", "Family", "Malayalam family dramas and gentler series.", true),
                new ExploreShelfDefinition("malayalam_shows_mystery", "Mystery", "Malayalam mystery-led series with strong metadata.", true),
            ]),
        new(
            "english-tv",
            "English TV Shows",
            "English TV Explore",
            "live_tv",
            "Curated English TV shelves built around rotating spotlight mixes, explicit latest lanes, recent additions, and genre-led discovery.",
            [
                new ExploreShelfDefinition("english_shows_recent", "Spotlight Mix", "A high-rotation blend of fresh, beloved, and underplayed English shows."),
                new ExploreShelfDefinition("english_shows_latest", "Wildcard Rotation", "A broader English TV swing that mixes newer series, older favorites, and surprise pulls."),
                new ExploreShelfDefinition("english_shows_recently_added", "Recently Added", "Strict newest-by-library-addition English series for when you want the freshest arrivals."),
                new ExploreShelfDefinition("english_shows_latest_releases", "Latest", "Strict newest-by-release English series with cleaner metadata."),
                new ExploreShelfDefinition("english_shows_romance", "Romance and Love", "English series with romance-forward metadata.", true),
                new ExploreShelfDefinition("english_shows_thriller", "Thriller and Suspense", "English thrillers, mysteries, and suspense picks.", true),
                new ExploreShelfDefinition("english_shows_action", "Action and Adventure", "English shows with action and adventure metadata.", true),
                new ExploreShelfDefinition("english_shows_comedy", "Comedy", "English comedy series and comfort rewatches.", true),
                new ExploreShelfDefinition("english_shows_crime", "Crime and Mystery", "English crime, detective, and mystery series.", true),
                new ExploreShelfDefinition("english_shows_family", "Family", "English family-friendly series and softer drama picks.", true),
                new ExploreShelfDefinition("english_shows_mystery", "Mystery", "Mystery-first English series with clean metadata.", true),
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
    string Description,
    bool IsGenreShelf = false);
