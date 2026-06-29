using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class Arguments
{
    [JsonPropertyName("game")]
    public List<ArgumentEntry> Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<ArgumentEntry> Jvm { get; set; }

    #nullable enable
    [JsonPropertyName("default-user-jvm")]
    public List<ArgumentEntry>? DefaultJvm { get; set; }
    #nullable disable
}