using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CuratedHome.Configuration;
using Jellyfin.Plugin.CuratedHome.Model;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CuratedHome.Services;

/// <summary>
/// Resolves Velvet Rows shelves into Jellyfin DTO results.
/// </summary>
public sealed class CuratedSectionResultsProvider
{
    private enum MixFlavor
    {
        Spotlight,
        Wildcard,
        Genre,
    }

    private static readonly string[] DefaultMalayalamTerms = ["malayalam", "മലയാളം"];
    private static readonly string[] RomanceGenres = ["romance", "romantic", "love"];
    private static readonly string[] ThrillerGenres = ["thriller", "suspense", "mystery", "psychological thriller", "crime thriller"];
    private static readonly string[] ActionGenres = ["action", "adventure", "action & adventure", "martial arts", "war"];
    private static readonly string[] ComedyGenres = ["comedy", "sitcom", "romantic comedy", "dark comedy"];
    private static readonly string[] CrimeGenres = ["crime", "crime thriller", "gangster", "detective", "police procedural", "investigation"];
    private static readonly string[] FamilyGenres = ["family", "children", "kids", "coming-of-age"];
    private static readonly string[] MysteryGenres = ["mystery", "detective", "whodunit", "supernatural mystery"];
    private static readonly string[] TitlePoisonMarkers =
    [
        "www.",
        ".com",
        ".mkv",
        ".mp4",
        "1080p",
        "720p",
        "hdrip",
        "web-dl",
        "webdl",
        "x264",
        "h264",
        "aac",
        "dd+",
        "esub",
        "atmos",
        "full movie",
    ];

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<CuratedSectionResultsProvider> _logger;

    private sealed record CuratedCandidate(
        BaseItem Item,
        DateTime ReleaseSortDate,
        DateTime AddedSortDate,
        double Rating,
        int Year);

    /// <summary>
    /// Initializes a new instance of the <see cref="CuratedSectionResultsProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The Jellyfin library manager.</param>
    /// <param name="userManager">The Jellyfin user manager.</param>
    /// <param name="dtoService">The DTO mapper used to build API results.</param>
    /// <param name="logger">The logger for curated row resolution.</param>
    public CuratedSectionResultsProvider(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService,
        ILogger<CuratedSectionResultsProvider> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the requested shelf payload into a Jellyfin query result.
    /// </summary>
    /// <param name="payload">The requesting user and shelf key.</param>
    /// <returns>The items for the requested shelf.</returns>
    public QueryResult<BaseItemDto> GetResults(SectionRequest payload)
    {
        var user = _userManager.GetUserById(payload.UserId);
        if (user is null)
        {
            return new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>());
        }

        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var dtoOptions = BuildDtoOptions();
        var limit = Math.Clamp(config.RowLimit, 1, 50);
        var shelfKey = payload.AdditionalData ?? string.Empty;

        IEnumerable<BaseItem> items = shelfKey switch
        {
            "malayalam_movies_recent" => GetSpotlightMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey),
            "malayalam_movies_latest" => GetWildcardMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey),
            "malayalam_movies_recently_added" => GetRecentlyAddedMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit),
            "malayalam_movies_latest_releases" => GetLatestMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit),
            "malayalam_movies_romance" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, RomanceGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_thriller" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, ThrillerGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_action" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, ActionGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_comedy" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, ComedyGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_crime" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, CrimeGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_family" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, FamilyGenres) : Array.Empty<BaseItem>(),
            "malayalam_movies_mystery" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit, shelfKey, MysteryGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_recent" => GetSpotlightShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey),
            "malayalam_shows_latest" => GetWildcardShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey),
            "malayalam_shows_recently_added" => GetRecentlyAddedShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true),
            "malayalam_shows_latest_releases" => GetLatestShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true),
            "malayalam_shows_romance" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, RomanceGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_thriller" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, ThrillerGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_action" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, ActionGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_comedy" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, ComedyGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_crime" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, CrimeGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_family" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, FamilyGenres) : Array.Empty<BaseItem>(),
            "malayalam_shows_mystery" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true, shelfKey, MysteryGenres) : Array.Empty<BaseItem>(),
            "english_movies_recent" => GetSpotlightMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey),
            "english_movies_latest" => GetWildcardMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey),
            "english_movies_recently_added" => GetRecentlyAddedMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_movies_latest_releases" => GetLatestMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_movies_romance" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, RomanceGenres) : Array.Empty<BaseItem>(),
            "english_movies_thriller" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, ThrillerGenres) : Array.Empty<BaseItem>(),
            "english_movies_action" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, ActionGenres) : Array.Empty<BaseItem>(),
            "english_movies_comedy" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, ComedyGenres) : Array.Empty<BaseItem>(),
            "english_movies_crime" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, CrimeGenres) : Array.Empty<BaseItem>(),
            "english_movies_family" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, FamilyGenres) : Array.Empty<BaseItem>(),
            "english_movies_mystery" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, shelfKey, MysteryGenres) : Array.Empty<BaseItem>(),
            "english_shows_recent" => GetSpotlightShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey),
            "english_shows_latest" => GetWildcardShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey),
            "english_shows_recently_added" => GetRecentlyAddedShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "english_shows_latest_releases" => GetLatestShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "english_shows_romance" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, RomanceGenres) : Array.Empty<BaseItem>(),
            "english_shows_thriller" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, ThrillerGenres) : Array.Empty<BaseItem>(),
            "english_shows_action" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, ActionGenres) : Array.Empty<BaseItem>(),
            "english_shows_comedy" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, ComedyGenres) : Array.Empty<BaseItem>(),
            "english_shows_crime" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, CrimeGenres) : Array.Empty<BaseItem>(),
            "english_shows_family" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, FamilyGenres) : Array.Empty<BaseItem>(),
            "english_shows_mystery" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, shelfKey, MysteryGenres) : Array.Empty<BaseItem>(),
            _ => Array.Empty<BaseItem>(),
        };

        var dtoItems = items
            .Select(x => _dtoService.GetBaseItemDto(x, dtoOptions, user))
            .ToArray();

        return new QueryResult<BaseItemDto>(dtoItems);
    }

    private DtoOptions BuildDtoOptions()
    {
        return new DtoOptions
        {
            Fields =
            [
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.Path,
                ItemFields.DateCreated,
            ],
            EnableImages = true,
            ImageTypeLimit = 1,
            ImageTypes =
            [
                ImageType.Primary,
                ImageType.Thumb,
                ImageType.Backdrop,
            ],
        };
    }

    private IEnumerable<BaseItem> GetSpotlightMovies(Guid userId, string configuredLibraryIds, int limit, string shelfKey)
    {
        return BuildMovieMix(userId, configuredLibraryIds, limit, shelfKey, MixFlavor.Spotlight);
    }

    private IEnumerable<BaseItem> GetWildcardMovies(Guid userId, string configuredLibraryIds, int limit, string shelfKey)
    {
        return BuildMovieMix(userId, configuredLibraryIds, limit, shelfKey, MixFlavor.Wildcard);
    }

    private IEnumerable<BaseItem> GetRecentlyAddedMovies(Guid userId, string configuredLibraryIds, int limit)
    {
        return SelectSortedItems(GetMovieCandidates(userId, configuredLibraryIds), limit, candidate => candidate.AddedSortDate);
    }

    private IEnumerable<BaseItem> GetLatestMovies(Guid userId, string configuredLibraryIds, int limit)
    {
        return SelectSortedItems(GetMovieCandidates(userId, configuredLibraryIds), limit, candidate => candidate.ReleaseSortDate);
    }

    private IEnumerable<BaseItem> GetSpotlightShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms, string shelfKey)
    {
        return BuildShowMix(userId, configuredLibraryIds, configuredMatchTerms, limit, useDefaultMalayalamTerms, shelfKey, MixFlavor.Spotlight);
    }

    private IEnumerable<BaseItem> GetWildcardShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms, string shelfKey)
    {
        return BuildShowMix(userId, configuredLibraryIds, configuredMatchTerms, limit, useDefaultMalayalamTerms, shelfKey, MixFlavor.Wildcard);
    }

    private IEnumerable<BaseItem> GetRecentlyAddedShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms)
    {
        return SelectSortedItems(GetShowCandidates(userId, configuredLibraryIds, configuredMatchTerms, useDefaultMalayalamTerms), limit, candidate => candidate.AddedSortDate);
    }

    private IEnumerable<BaseItem> GetLatestShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms)
    {
        return SelectSortedItems(GetShowCandidates(userId, configuredLibraryIds, configuredMatchTerms, useDefaultMalayalamTerms), limit, candidate => candidate.ReleaseSortDate);
    }

    private IEnumerable<BaseItem> GetGenreMovies(Guid userId, string configuredLibraryIds, int limit, string shelfKey, string[] genres)
    {
        return BuildMovieMix(userId, configuredLibraryIds, limit, shelfKey, MixFlavor.Genre, genres);
    }

    private IEnumerable<BaseItem> GetGenreShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms, string shelfKey, string[] genres)
    {
        return BuildEpicMix(GetShowCandidates(userId, configuredLibraryIds, configuredMatchTerms, useDefaultMalayalamTerms, genres), limit, shelfKey, MixFlavor.Genre);
    }

    private IEnumerable<BaseItem> BuildMovieMix(Guid userId, string configuredLibraryIds, int limit, string shelfKey, MixFlavor flavor, IReadOnlyCollection<string>? genres = null)
    {
        return BuildEpicMix(GetMovieCandidates(userId, configuredLibraryIds, genres), limit, shelfKey, flavor);
    }

    private IEnumerable<BaseItem> BuildShowMix(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms, string shelfKey, MixFlavor flavor)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "tvshows", configuredLibraryIds);
        var seriesItems = folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                Limit = Math.Max(limit * 40, 320),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .OfType<Series>()
            .Where(IsCuratedMetadataSafe)
            .Where(x => MatchesConfiguredTerms(x, configuredMatchTerms, useDefaultMalayalamTerms))
            .DistinctBy(x => x.Id);

        return BuildEpicMix(seriesItems.Select(CreateCandidate), limit, shelfKey, flavor);
    }

    // Interleave fresh, beloved, classic, and wildcard pools so the rows keep rotating without collapsing into random noise.
    private IEnumerable<BaseItem> BuildEpicMix(IEnumerable<CuratedCandidate> candidates, int limit, string shelfKey, MixFlavor flavor)
    {
        var candidateArray = candidates.ToArray();

        if (candidateArray.Length == 0)
        {
            return Array.Empty<BaseItem>();
        }

        if (candidateArray.Length <= limit)
        {
            return Shuffle(candidateArray, CreateShelfRandom(shelfKey, flavor))
                .Select(x => x.Item)
                .ToArray();
        }

        var poolSize = Math.Min(candidateArray.Length, Math.Max(limit * 6, 36));
        var freshPool = Shuffle(
                candidateArray
                    .OrderByDescending(x => x.ReleaseSortDate)
                    .ThenByDescending(x => x.AddedSortDate)
                    .Take(poolSize),
                CreateShelfRandom($"{shelfKey}:fresh", flavor))
            .ToArray();
        var belovedSource = candidateArray
            .Where(x => x.Rating > 0)
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.ReleaseSortDate)
            .Take(poolSize)
            .ToArray();
        var belovedPool = Shuffle(
                belovedSource.Length > 0
                    ? belovedSource
                    : candidateArray.OrderByDescending(x => x.ReleaseSortDate).Take(poolSize),
                CreateShelfRandom($"{shelfKey}:beloved", flavor))
            .ToArray();
        var classicPool = Shuffle(
                candidateArray
                    .OrderBy(x => x.ReleaseSortDate)
                    .ThenBy(x => x.AddedSortDate)
                    .Take(poolSize),
                CreateShelfRandom($"{shelfKey}:classic", flavor))
            .ToArray();
        var wildcardPool = Shuffle(candidateArray, CreateShelfRandom($"{shelfKey}:wildcard", flavor))
            .Take(poolSize)
            .ToArray();

        var pools = flavor switch
        {
            MixFlavor.Spotlight => new[] { freshPool, belovedPool, wildcardPool, classicPool },
            MixFlavor.Wildcard => new[] { wildcardPool, classicPool, belovedPool, freshPool },
            _ => new[] { belovedPool, freshPool, wildcardPool, classicPool },
        };

        return SelectVariedItems(pools, candidateArray, limit, CreateShelfRandom($"{shelfKey}:fallback", flavor));
    }

    private IEnumerable<BaseItem> SelectSortedItems(IEnumerable<CuratedCandidate> candidates, int limit, Func<CuratedCandidate, DateTime> primarySort)
    {
        return candidates
            .OrderByDescending(primarySort)
            .ThenByDescending(candidate => candidate.ReleaseSortDate)
            .ThenByDescending(candidate => candidate.AddedSortDate)
            .ThenByDescending(candidate => candidate.Rating)
            .Select(candidate => candidate.Item)
            .Take(limit)
            .ToArray();
    }

    private IEnumerable<BaseItem> SelectVariedItems(IEnumerable<CuratedCandidate[]> pools, CuratedCandidate[] allCandidates, int limit, Random fallbackRandom)
    {
        var queues = pools
            .Select(pool => new Queue<CuratedCandidate>(pool))
            .ToArray();
        var selected = new List<BaseItem>(limit);
        var usedIds = new HashSet<Guid>();
        var recentYears = new Queue<int>();

        while (selected.Count < limit && queues.Any(queue => queue.Count > 0))
        {
            var addedThisPass = false;

            foreach (var queue in queues)
            {
                if (selected.Count >= limit)
                {
                    break;
                }

                if (!TryDequeueVaried(queue, usedIds, recentYears, out var candidate))
                {
                    continue;
                }

                usedIds.Add(candidate.Item.Id);
                selected.Add(candidate.Item);
                TrackRecentYear(recentYears, candidate.Year);
                addedThisPass = true;
            }

            if (!addedThisPass)
            {
                break;
            }
        }

        foreach (var candidate in Shuffle(allCandidates, fallbackRandom))
        {
            if (selected.Count >= limit)
            {
                break;
            }

            if (usedIds.Add(candidate.Item.Id))
            {
                selected.Add(candidate.Item);
            }
        }

        return selected;
    }

    private bool TryDequeueVaried(Queue<CuratedCandidate> queue, HashSet<Guid> usedIds, Queue<int> recentYears, out CuratedCandidate candidate)
    {
        var initialCount = queue.Count;
        for (var index = 0; index < initialCount; index++)
        {
            var next = queue.Dequeue();
            if (usedIds.Contains(next.Item.Id))
            {
                continue;
            }

            if (next.Year > 0 && recentYears.Contains(next.Year) && index < initialCount - 1)
            {
                queue.Enqueue(next);
                continue;
            }

            candidate = next;
            return true;
        }

        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (usedIds.Contains(next.Item.Id))
            {
                continue;
            }

            candidate = next;
            return true;
        }

        candidate = default!;
        return false;
    }

    private void TrackRecentYear(Queue<int> recentYears, int year)
    {
        if (year <= 0)
        {
            return;
        }

        recentYears.Enqueue(year);
        while (recentYears.Count > 3)
        {
            recentYears.Dequeue();
        }
    }

    private CuratedCandidate CreateCandidate(BaseItem item)
    {
        var addedSortDate = item.DateCreated == default ? DateTime.UnixEpoch : item.DateCreated.ToUniversalTime();
        var releaseSortDate = GetTrustedReleaseSortDate(item)?.ToUniversalTime() ?? addedSortDate;
        var year = item.ProductionYear ?? (releaseSortDate > DateTime.UnixEpoch ? releaseSortDate.Year : 0);

        return new CuratedCandidate(
            item,
            releaseSortDate,
            addedSortDate,
            Convert.ToDouble(item.CommunityRating ?? 0),
            year);
    }

    private IEnumerable<CuratedCandidate> GetMovieCandidates(Guid userId, string configuredLibraryIds, IReadOnlyCollection<string>? genres = null)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<CuratedCandidate>();
        }

        var folders = GetConfiguredLibraries(userId, "movies", configuredLibraryIds);
        var items = folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                Limit = 480,
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .Where(IsCuratedMetadataSafe)
            .DistinctBy(x => x.Id);

        if (genres is not null && genres.Count > 0)
        {
            items = items.Where(x => MatchesGenres(x, genres));
        }

        return items.Select(CreateCandidate);
    }

    private IEnumerable<CuratedCandidate> GetShowCandidates(Guid userId, string configuredLibraryIds, string configuredMatchTerms, bool useDefaultMalayalamTerms, IReadOnlyCollection<string>? genres = null)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<CuratedCandidate>();
        }

        var folders = GetConfiguredLibraries(userId, "tvshows", configuredLibraryIds);
        var seriesItems = folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                Limit = 320,
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .OfType<Series>()
            .Where(IsCuratedMetadataSafe)
            .Where(x => MatchesConfiguredTerms(x, configuredMatchTerms, useDefaultMalayalamTerms))
            .DistinctBy(x => x.Id);

        if (genres is not null && genres.Count > 0)
        {
            seriesItems = seriesItems.Where(x => MatchesGenres(x, genres));
        }

        return seriesItems.Select(CreateCandidate);
    }

    private Random CreateShelfRandom(string shelfKey, MixFlavor flavor)
    {
        return new Random(HashCode.Combine(shelfKey, flavor, Guid.NewGuid(), DateTime.UtcNow.Ticks));
    }

    private static T[] Shuffle<T>(IEnumerable<T> source, Random random)
    {
        var items = source.ToArray();
        for (var index = items.Length - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }

        return items;
    }

    private bool MatchesConfiguredTerms(BaseItem item, string configuredMatchTerms, bool useDefaultMalayalamTerms)
    {
        var searchTerms = configuredMatchTerms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (searchTerms.Length == 0)
        {
            searchTerms = useDefaultMalayalamTerms ? DefaultMalayalamTerms : Array.Empty<string>();
        }

        if (searchTerms.Length == 0)
        {
            return true;
        }

        var searchableParts = new List<string>
        {
            item.Name ?? string.Empty,
            item.OriginalTitle ?? string.Empty,
            item.Path ?? string.Empty,
            item.Overview ?? string.Empty,
            item.Tagline ?? string.Empty,
        };

        searchableParts.AddRange(item.Genres ?? []);
        searchableParts.AddRange(item.Tags ?? []);
        searchableParts.AddRange(item.Studios ?? []);

        var searchable = string.Join('\n', searchableParts.Where(x => !string.IsNullOrWhiteSpace(x)));
        return searchTerms.Any(term => searchable.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesGenres(BaseItem item, IReadOnlyCollection<string> genres)
    {
        if (genres.Count == 0 || item.Genres.Length == 0)
        {
            return false;
        }

        return item.Genres.Any(itemGenre =>
            genres.Any(genre =>
                itemGenre.Contains(genre, StringComparison.OrdinalIgnoreCase) ||
                genre.Contains(itemGenre, StringComparison.OrdinalIgnoreCase)));
    }

    private Folder[] GetConfiguredLibraries(Guid userId, string expectedCollectionType, string configuredLibraryIds)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return [];
        }

        var configuredIds = configuredLibraryIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var guid) ? guid : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();

        var folders = _libraryManager.GetUserRootFolder()
            .GetChildren(user, true)
            .OfType<Folder>()
            .Where(x => string.Equals(
                (x as ICollectionFolder)?.CollectionType?.ToString(),
                expectedCollectionType,
                StringComparison.OrdinalIgnoreCase));

        if (configuredIds.Count == 0)
        {
            return folders.ToArray();
        }

        var matched = folders
            .Where(x => configuredIds.Contains(x.Id))
            .ToArray();

        if (matched.Length == 0)
        {
            _logger.LogWarning("No libraries matched configured ids '{Libraries}' for collection type '{CollectionType}'", configuredLibraryIds, expectedCollectionType);
        }

        return matched;
    }

    private bool IsCuratedMetadataSafe(BaseItem item)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!config.HideLowConfidenceTitles)
        {
            return !string.IsNullOrWhiteSpace(item.Name);
        }

        var name = (item.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lowered = name.ToLowerInvariant();
        var hasPoisonMarker = TitlePoisonMarkers.Any(lowered.Contains);
        if (!hasPoisonMarker)
        {
            return true;
        }

        if (lowered.StartsWith("www.", StringComparison.Ordinal) || lowered.Contains(".com", StringComparison.Ordinal))
        {
            return false;
        }

        if (lowered.Contains("full movie", StringComparison.Ordinal) || lowered.EndsWith(".mkv", StringComparison.Ordinal) || lowered.EndsWith(".mp4", StringComparison.Ordinal))
        {
            return false;
        }

        return item.PremiereDate.HasValue || item.ProductionYear.HasValue;
    }

    private DateTime? GetTrustedReleaseSortDate(BaseItem item)
    {
        if (item.PremiereDate.HasValue)
        {
            return item.PremiereDate.Value;
        }

        if (item.ProductionYear.HasValue && item.ProductionYear.Value > 0)
        {
            return new DateTime(item.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        return null;
    }
}
