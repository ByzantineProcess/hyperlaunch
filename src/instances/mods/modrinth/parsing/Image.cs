using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class Image
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("featured")]
    public required bool Featured { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created")]
    public required string CreatedDate { get; set; }

    [JsonPropertyName("ordering")]
    public int Ordering { get; set; }

}