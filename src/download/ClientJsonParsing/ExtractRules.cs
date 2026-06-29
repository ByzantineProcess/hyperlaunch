using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class ExtractRules
{
    [JsonPropertyName("exclude")]
    public List<string> Exclude { get; set; }
}