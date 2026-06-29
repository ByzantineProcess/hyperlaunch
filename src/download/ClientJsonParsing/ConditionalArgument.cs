using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class ConditionalArgument
{
    [JsonPropertyName("rules")]
    public List<Rule> Rules { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(StringOrStringListConverter))]
    public List<string> Value { get; set; }
}