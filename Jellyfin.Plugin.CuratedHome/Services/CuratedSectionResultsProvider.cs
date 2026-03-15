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

public sealed class CuratedSectionResultsProvider
{
    private static readonly string[] DefaultMalayalamTerms = ["malayalam", "മലയാളം"];

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<CuratedSectionResultsProvider> _logger;

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
            "malayalam_movies_recent" => GetRecentlyAddedMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit),
            "malayalam_movies_latest" => GetLatestMovies(payload.UserId, config.MalayalamMovieLibraryIds, limit),
            "malayalam_shows_recent" => GetRecentlyAddedShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true),
            "malayalam_shows_latest" => GetLatestShows(payload.UserId, config.MalayalamTvLibraryIds, config.MalayalamTvMatchTerms, limit, true),
            "english_movies_recent" => GetRecentlyAddedMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_movies_latest" => GetLatestMovies(payload.UserId, config.EnglishMovieLibraryIds, limit),
            "english_shows_recent" => GetRecentlyAddedShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "english_shows_latest" => GetLatestShows(payload.UserId, config.EnglishTvLibraryIds, string.Empty, limit, false),
            "tamil_movies_recent" => GetRecentlyAddedMovies(payload.UserId, config.TamilMovieLibraryIds, limit),
            "tamil_movies_latest" => GetLatestMovies(payload.UserId, config.TamilMovieLibraryIds, limit),
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
            .DistinctBy(x => x.Id)
            .OrderByDescending(GetReleaseSortDate)
            .Take(limit);
    }

    private IEnumerable<BaseItem> GetRecentlyAddedShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms)
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
            .Where(x => MatchesConfiguredTerms(x.Series, configuredMatchTerms, useDefaultMalayalamTerms))
            .OrderByDescending(x => x.SortDate)
            .Take(limit)
            .ToArray();

        return ResolveSeriesInOrder(userId, seriesOrder.Select(x => x.Series.Id).ToArray());
    }

    private IEnumerable<BaseItem> GetLatestShows(Guid userId, string configuredLibraryIds, string configuredMatchTerms, int limit, bool useDefaultMalayalamTerms)
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
            .Where(x => MatchesConfiguredTerms(x, configuredMatchTerms, useDefaultMalayalamTerms))
            .DistinctBy(x => x.Id)
            .OrderByDescending(GetReleaseSortDate)
            .Take(limit);
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

    private DateTime GetReleaseSortDate(BaseItem item)
    {
        if (item.PremiereDate.HasValue)
        {
            return item.PremiereDate.Value;
        }

        if (item.ProductionYear.HasValue && item.ProductionYear.Value > 0)
        {
            return new DateTime(item.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        return item.DateCreated;
    }
}
