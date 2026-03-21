using System.Security.Claims;
using System.Text.Encodings.Web;
using Jellyfin.Plugin.CuratedHome.Explore;
using Jellyfin.Plugin.CuratedHome.Model;
using Jellyfin.Plugin.CuratedHome.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CuratedHome.Controllers;

/// <summary>
/// Serves user-facing Velvet Rows explore pages and their shelf data.
/// </summary>
[ApiController]
[Route("VelvetRows")]
public sealed class ExplorePagesController : ControllerBase
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "JellyfinUserId",
        "Jellyfin-UserId",
        "UserId",
        "user_id",
        "uid",
    ];
    private static readonly string[] AuthorizationHeaderNames =
    [
        "Authorization",
        "X-Emby-Authorization",
    ];

    private readonly IApplicationPaths _applicationPaths;
    private readonly CuratedSectionResultsProvider _resultsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplorePagesController"/> class.
    /// </summary>
    /// <param name="applicationPaths">The Jellyfin application paths.</param>
    /// <param name="resultsProvider">The curated shelf data provider.</param>
    public ExplorePagesController(IApplicationPaths applicationPaths, CuratedSectionResultsProvider resultsProvider)
    {
        _applicationPaths = applicationPaths;
        _resultsProvider = resultsProvider;
    }

    /// <summary>
    /// Returns the embedded Velvet Rows explore page shell for a library group.
    /// </summary>
    /// <param name="page">The explore page key.</param>
    /// <returns>The rendered HTML fragment.</returns>
    [HttpGet("Page")]
    [Authorize]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetPage([FromQuery] string page)
    {
        var definition = ExplorePageCatalog.Find(page);
        if (definition is null)
        {
            return NotFound();
        }

        var resourcePath = $"{typeof(Plugin).Namespace}.Explore.explorePage.html";
        using var stream = typeof(Plugin).Assembly.GetManifestResourceStream(resourcePath);
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd()
            .Replace("__VELVET_PAGE_KEY__", HtmlEncoder.Default.Encode(definition.Key), StringComparison.Ordinal)
            .Replace("__VELVET_PAGE_TITLE__", HtmlEncoder.Default.Encode(definition.Title), StringComparison.Ordinal)
            .Replace("__VELVET_PAGE_DESCRIPTION__", HtmlEncoder.Default.Encode(definition.Description), StringComparison.Ordinal);

        return Content(html, "text/html");
    }

    /// <summary>
    /// Returns all shelf data needed by a Velvet Rows explore page.
    /// </summary>
    /// <param name="page">The explore page key.</param>
    /// <param name="userId">The optional requesting user id from Jellyfin clients.</param>
    /// <returns>The configured shelves and their resolved items.</returns>
    [HttpGet("ExploreData")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetExploreData([FromQuery] string page, [FromQuery] Guid? userId = null)
    {
        var definition = ExplorePageCatalog.Find(page);
        if (definition is null)
        {
            return NotFound();
        }

        var resolvedUserId = TryResolveCurrentUserId() ?? userId;
        if (!resolvedUserId.HasValue || resolvedUserId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        if (userId.HasValue && userId.Value != Guid.Empty && userId.Value != resolvedUserId.Value)
        {
            return Forbid();
        }

        var config = Plugin.Instance?.Configuration;
        var shelves = definition.Shelves
            .Where(shelf => (config?.EnableGenreShelves ?? true) || !shelf.IsGenreShelf)
            .Select(shelf => new
            {
                id = shelf.DataKey,
                title = shelf.Title,
                description = shelf.Description,
                items = _resultsProvider.GetResults(new SectionRequest
                {
                    UserId = resolvedUserId.Value,
                    AdditionalData = shelf.DataKey,
                }).Items,
            })
            .ToArray();

        return Ok(new
        {
            page = definition.Key,
            title = definition.Title,
            description = definition.Description,
            shelves,
        });
    }

    /// <summary>
    /// Resolves a Home Screen Sections custom row over HTTP.
    /// </summary>
    /// <param name="payload">The requesting user and section key.</param>
    /// <returns>The DTO results for the requested row.</returns>
    [HttpPost("HomeSectionResults")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<QueryResult<BaseItemDto>> GetHomeSectionResults([FromBody] SectionRequest payload)
    {
        var resolvedUserId = TryResolveCurrentUserId();
        if (resolvedUserId.HasValue && resolvedUserId.Value != Guid.Empty)
        {
            if (payload.UserId != Guid.Empty && payload.UserId != resolvedUserId.Value)
            {
                return Forbid();
            }

            payload.UserId = resolvedUserId.Value;
        }

        if (payload.UserId == Guid.Empty)
        {
            return BadRequest();
        }

        return Ok(_resultsProvider.GetResults(payload));
    }

    private Guid? TryResolveCurrentUserId()
    {
        foreach (var claimType in UserIdClaimTypes)
        {
            var claimValue = User.FindFirstValue(claimType);
            if (Guid.TryParse(claimValue, out var userId) && userId != Guid.Empty)
            {
                return userId;
            }
        }

        if (Guid.TryParse(User.Identity?.Name, out var identityUserId) && identityUserId != Guid.Empty)
        {
            return identityUserId;
        }

        return TryResolveUserIdFromAccessToken(TryResolveAccessToken()) ?? TryResolveUserIdFromAuthorizationHeader();
    }

    private string? TryResolveAccessToken()
    {
        if (Request.Headers.TryGetValue("X-Emby-Token", out var directTokenValues))
        {
            var directToken = directTokenValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(directToken))
            {
                return directToken;
            }
        }

        foreach (var headerName in AuthorizationHeaderNames)
        {
            if (!Request.Headers.TryGetValue(headerName, out var values))
            {
                continue;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var markerIndex = value.IndexOf("Token=\"", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                {
                    continue;
                }

                var tokenStart = markerIndex + "Token=\"".Length;
                var tokenEnd = value.IndexOf('"', tokenStart);
                if (tokenEnd < 0)
                {
                    continue;
                }

                var candidate = value[tokenStart..tokenEnd];
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private Guid? TryResolveUserIdFromAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var jellyfinDbPath = Path.GetFullPath(Path.Combine(_applicationPaths.PluginConfigurationsPath, "..", "..", "data", "jellyfin.db"));
        if (!System.IO.File.Exists(jellyfinDbPath))
        {
            return null;
        }

        using var connection = new SqliteConnection($"Data Source={jellyfinDbPath};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId FROM Devices WHERE AccessToken = $token ORDER BY DateLastActivity DESC LIMIT 1;";
        command.Parameters.AddWithValue("$token", accessToken);

        var resolved = command.ExecuteScalar()?.ToString();
        if (Guid.TryParse(resolved, out var userId) && userId != Guid.Empty)
        {
            return userId;
        }

        return null;
    }

    private Guid? TryResolveUserIdFromAuthorizationHeader()
    {
        foreach (var headerName in AuthorizationHeaderNames)
        {
            if (!Request.Headers.TryGetValue(headerName, out var values))
            {
                continue;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var markerIndex = value.IndexOf("UserId=\"", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                {
                    continue;
                }

                var idStart = markerIndex + "UserId=\"".Length;
                var idEnd = value.IndexOf('"', idStart);
                if (idEnd < 0)
                {
                    continue;
                }

                var candidate = value[idStart..idEnd];
                if (Guid.TryParse(candidate, out var headerUserId) && headerUserId != Guid.Empty)
                {
                    return headerUserId;
                }
            }
        }

        return null;
    }
}
