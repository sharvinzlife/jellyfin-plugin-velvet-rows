namespace Jellyfin.Plugin.CuratedHome.Model;

public sealed class SectionRequest
{
    public Guid UserId { get; set; }

    public string? AdditionalData { get; set; }
}
