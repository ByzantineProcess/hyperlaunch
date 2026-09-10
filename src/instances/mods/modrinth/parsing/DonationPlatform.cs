using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class DonationPlatform
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("platform")]
    public required string Platform { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}