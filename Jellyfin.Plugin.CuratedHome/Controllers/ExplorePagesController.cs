using System.Text.Encodings.Web;
using Jellyfin.Plugin.CuratedHome.Configuration;
using Jellyfin.Plugin.CuratedHome.Explore;
using Jellyfin.Plugin.CuratedHome.Model;
using Jellyfin.Plugin.CuratedHome.Services;
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
    private readonly CuratedSectionResultsProvider _resultsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplorePagesController"/> class.
    /// </summary>
    /// <param name="resultsProvider">The curated shelf data provider.</param>
    public ExplorePagesController(CuratedSectionResultsProvider resultsProvider)
    {
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
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var definition = ExplorePageCatalog.Find(page, config);
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
    /// <param name="userId">The requesting user id.</param>
    /// <returns>The configured shelves and their resolved items.</returns>
    [HttpGet("ExploreData")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetExploreData([FromQuery] string page, [FromQuery] Guid userId)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var definition = ExplorePageCatalog.Find(page, config);
        if (definition is null)
        {
            return NotFound();
        }

        var shelves = definition.Shelves
            .Where(shelf => config.EnableGenreShelves || !IsGenreShelf(shelf.DataKey))
            .Select(shelf => new
            {
                id = shelf.DataKey,
                title = shelf.Title,
                description = shelf.Description,
                items = _resultsProvider.GetResults(new SectionRequest
                {
                    UserId = userId,
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

    private static bool IsGenreShelf(string dataKey)
    {
        return dataKey.EndsWith("_romance", StringComparison.Ordinal)
            || dataKey.EndsWith("_thriller", StringComparison.Ordinal)
            || dataKey.EndsWith("_action", StringComparison.Ordinal)
            || dataKey.EndsWith("_comedy", StringComparison.Ordinal)
            || dataKey.EndsWith("_crime", StringComparison.Ordinal)
            || dataKey.EndsWith("_family", StringComparison.Ordinal)
            || dataKey.EndsWith("_mystery", StringComparison.Ordinal);
    }
}
