
using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

public class Dependency
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }

    [JsonPropertyName("dependency_type")]
    public string DependencyType { get; set; } // TODO: this looks really important, map to enum?

    [JsonPropertyName("name")]
    public string Name { get; set; }

    #nullable enable

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
}