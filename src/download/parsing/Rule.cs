using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class Rule
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    #nullable enable
    [JsonPropertyName("features")]
    public RuleFeatures? Features { get; set; }

    [JsonPropertyName("os")]
    public RuleOs? Os { get; set; }
    #nullable disable
}