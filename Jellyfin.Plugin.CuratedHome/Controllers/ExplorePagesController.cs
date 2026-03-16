using System.Text.Encodings.Web;
using Jellyfin.Plugin.CuratedHome.Explore;
using Jellyfin.Plugin.CuratedHome.Model;
using Jellyfin.Plugin.CuratedHome.Services;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
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
    /// <param name="userId">The requesting user id.</param>
    /// <returns>The configured shelves and their resolved items.</returns>
    [HttpGet("ExploreData")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetExploreData([FromQuery] string page, [FromQuery] Guid userId)
    {
        var definition = ExplorePageCatalog.Find(page);
        if (definition is null)
        {
            return NotFound();
        }

        var config = Plugin.Instance?.Configuration;
        var shelves = definition.Shelves
            .Where(shelf => (config?.EnableGenreShelves ?? true) || !shelf.DataKey.Contains("_romance", StringComparison.Ordinal) && !shelf.DataKey.Contains("_thriller", StringComparison.Ordinal) && !shelf.DataKey.Contains("_action", StringComparison.Ordinal))
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

    /// <summary>
    /// Resolves a Home Screen Sections custom row over HTTP.
    /// </summary>
    /// <param name="payload">The requesting user and section key.</param>
    /// <returns>The DTO results for the requested row.</returns>
    [HttpPost("HomeSectionResults")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetHomeSectionResults([FromBody] SectionRequest payload)
    {
        return Ok(_resultsProvider.GetResults(payload));
    }
}
