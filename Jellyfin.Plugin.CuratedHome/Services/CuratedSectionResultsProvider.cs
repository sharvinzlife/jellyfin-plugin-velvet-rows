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
    private static readonly string[] DefaultFocusedTerms = ["malayalam", "മലയാളം"];
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

        IEnumerable<BaseItem> items = payload.AdditionalData switch
        {
            "featured_movies_recent" => GetRecentlyAddedMovies(payload.UserId, config.FocusedMovieLibraryIds, limit),
            "featured_movies_latest" => GetLatestMovies(payload.UserId, config.FocusedMovieLibraryIds, limit),
            "featured_movies_romance" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, RomanceGenres) : Array.Empty<BaseItem>(),
            "featured_movies_thriller" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, ThrillerGenres) : Array.Empty<BaseItem>(),
            "featured_movies_action" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, ActionGenres) : Array.Empty<BaseItem>(),
            "featured_movies_comedy" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, ComedyGenres) : Array.Empty<BaseItem>(),
            "featured_movies_crime" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, CrimeGenres) : Array.Empty<BaseItem>(),
            "featured_movies_family" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, FamilyGenres) : Array.Empty<BaseItem>(),
            "featured_movies_mystery" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.FocusedMovieLibraryIds, limit, MysteryGenres) : Array.Empty<BaseItem>(),
            "featured_shows_recent" => GetRecentlyAddedShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true),
            "featured_shows_latest" => GetLatestShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true),
            "featured_shows_romance" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, RomanceGenres) : Array.Empty<BaseItem>(),
            "featured_shows_thriller" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, ThrillerGenres) : Array.Empty<BaseItem>(),
            "featured_shows_action" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, ActionGenres) : Array.Empty<BaseItem>(),
            "featured_shows_comedy" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, ComedyGenres) : Array.Empty<BaseItem>(),
            "featured_shows_crime" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, CrimeGenres) : Array.Empty<BaseItem>(),
            "featured_shows_family" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, FamilyGenres) : Array.Empty<BaseItem>(),
            "featured_shows_mystery" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.FocusedTvLibraryIds, config.FocusedTvMatchTerms, limit, true, MysteryGenres) : Array.Empty<BaseItem>(),
            "english_movies_recent" => GetRecentlyAddedMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_movies_latest" => GetLatestMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_movies_romance" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, RomanceGenres) : Array.Empty<BaseItem>(),
            "english_movies_thriller" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, ThrillerGenres) : Array.Empty<BaseItem>(),
            "english_movies_action" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, ActionGenres) : Array.Empty<BaseItem>(),
            "english_movies_comedy" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, ComedyGenres) : Array.Empty<BaseItem>(),
            "english_movies_crime" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, CrimeGenres) : Array.Empty<BaseItem>(),
            "english_movies_family" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, FamilyGenres) : Array.Empty<BaseItem>(),
            "english_movies_mystery" => config.EnableGenreShelves ? GetGenreMovies(payload.UserId, config.EnglishMovieLibraryIds, limit, MysteryGenres) : Array.Empty<BaseItem>(),
            "english_shows_recent" => GetRecentlyAddedShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "english_shows_latest" => GetLatestShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "english_shows_romance" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, RomanceGenres) : Array.Empty<BaseItem>(),
            "english_shows_thriller" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, ThrillerGenres) : Array.Empty<BaseItem>(),
            "english_shows_action" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, ActionGenres) : Array.Empty<BaseItem>(),
            "english_shows_comedy" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, ComedyGenres) : Array.Empty<BaseItem>(),
            "english_shows_crime" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, CrimeGenres) : Array.Empty<BaseItem>(),
            "english_shows_family" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, FamilyGenres) : Array.Empty<BaseItem>(),
            "english_shows_mystery" => config.EnableGenreShelves ? GetGenreShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false, MysteryGenres) : Array.Empty<BaseItem>(),
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

    private IEnumerable<BaseItem> GetRecentlyAddedMovies(Guid userId, string configuredLibraryIds, int limit)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "movies", configuredLibraryIds);
        return folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                Limit = Math.Max(limit * 20, 256),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .Where(IsCuratedMetadataSafe)
            .DistinctBy(x => x.Id)
            .OrderByDescending(x => x.DateCreated)
            .Take(limit);
    }

    private IEnumerable<BaseItem> GetLatestMovies(Guid userId, string configuredLibraryIds, int limit)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "movies", configuredLibraryIds);
        return folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                Limit = Math.Max(limit * 20, 256),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .Where(IsCuratedMetadataSafe)
            .DistinctBy(x => x.Id)
            .Select(x => new
            {
                Item = x,
                SortDate = GetTrustedReleaseSortDate(x),
            })
            .Where(x => x.SortDate.HasValue)
            .OrderByDescending(x => x.SortDate!.Value)
            .Select(x => x.Item)
            .Take(limit);
    }

    private IEnumerable<BaseItem> GetRecentlyAddedShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultFocusedTerms)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "tvshows", configuredLibraryIds);
        var recentEpisodes = folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Episode],
                Limit = Math.Max(limit * 24, 256),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .OfType<Episode>()
            .Where(x => x.Series is not null)
            .ToList();

        var seriesOrder = recentEpisodes
            .GroupBy(x => x.Series!.Id)
            .Select(g => new
            {
                Series = g.First().Series!,
                SortDate = g.Max(x => x.DateCreated),
            })
            .Where(x => MatchesConfiguredTerms(x.Series, configuredMatchTerms, useDefaultFocusedTerms))
            .Where(x => IsCuratedMetadataSafe(x.Series))
            .OrderByDescending(x => x.SortDate)
            .Take(limit)
            .ToArray();

        return ResolveSeriesInOrder(userId, seriesOrder.Select(x => x.Series.Id).ToArray());
    }

    private IEnumerable<BaseItem> GetLatestShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultFocusedTerms)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "tvshows", configuredLibraryIds);
        return folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                Limit = Math.Max(limit * 12, 128),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .OfType<Series>()
            .Where(x => MatchesConfiguredTerms(x, configuredMatchTerms, useDefaultFocusedTerms))
            .Where(IsCuratedMetadataSafe)
            .DistinctBy(x => x.Id)
            .Select(x => new
            {
                Item = x,
                SortDate = GetTrustedReleaseSortDate(x),
            })
            .Where(x => x.SortDate.HasValue)
            .OrderByDescending(x => x.SortDate!.Value)
            .Select(x => x.Item)
            .Take(limit);
    }

    private IEnumerable<BaseItem> GetGenreMovies(Guid userId, string configuredLibraryIds, int limit, string[] genres)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "movies", configuredLibraryIds);
        return folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                Limit = Math.Max(limit * 30, 320),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .Where(IsCuratedMetadataSafe)
            .Where(x => MatchesGenres(x, genres))
            .DistinctBy(x => x.Id)
            .Select(x => new
            {
                Item = x,
                SortDate = GetTrustedReleaseSortDate(x),
            })
            .Where(x => x.SortDate.HasValue)
            .OrderByDescending(x => x.SortDate!.Value)
            .Select(x => x.Item)
            .Take(limit);
    }

    private IEnumerable<BaseItem> GetGenreShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultFocusedTerms, string[] genres)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Array.Empty<BaseItem>();
        }

        var folders = GetConfiguredLibraries(userId, "tvshows", configuredLibraryIds);
        return folders
            .SelectMany(folder => folder.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                Limit = Math.Max(limit * 20, 240),
                Recursive = true,
                ParentId = folder.Id,
                IsMissing = false,
            }).Items)
            .OfType<Series>()
            .Where(IsCuratedMetadataSafe)
            .Where(x => MatchesConfiguredTerms(x, configuredMatchTerms, useDefaultFocusedTerms))
            .Where(x => MatchesGenres(x, genres))
            .DistinctBy(x => x.Id)
            .Select(x => new
            {
                Item = x,
                SortDate = GetTrustedReleaseSortDate(x),
            })
            .Where(x => x.SortDate.HasValue)
            .OrderByDescending(x => x.SortDate!.Value)
            .Select(x => x.Item)
            .Take(limit);
    }

    private bool MatchesConfiguredTerms(BaseItem item, string configuredMatchTerms, bool useDefaultFocusedTerms)
    {
        var searchTerms = configuredMatchTerms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (searchTerms.Length == 0)
        {
            searchTerms = useDefaultFocusedTerms ? DefaultFocusedTerms : Array.Empty<string>();
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

    private IEnumerable<BaseItem> ResolveSeriesInOrder(Guid userId, Guid[] ids)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null || ids.Length == 0)
        {
            return Array.Empty<BaseItem>();
        }

        var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            ItemIds = ids,
        });

        return ids
            .Select(id => items.FirstOrDefault(x => x.Id == id))
            .Where(x => x is not null)
            .Cast<BaseItem>();
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
