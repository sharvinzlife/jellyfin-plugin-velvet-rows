namespace Jellyfin.Plugin.CuratedHome.Model;

/// <summary>
/// Payload passed from Home Screen Sections into Velvet Rows result handlers.
/// </summary>
public sealed class SectionRequest
{
    /// <summary>
    /// Gets or sets the requesting Jellyfin user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the additional section key registered for the request.
    /// </summary>
    public string? AdditionalData { get; set; }
}
