using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class RuleOs
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    #nullable enable
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }

    [JsonPropertyName("versionRange")]
    public VersionRange? VersionRange { get; set; }
}