using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class Licence
{
    // docs are ambiguous on whether id and name are nullable?

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}