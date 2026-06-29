using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class JavaVersionInfo
{
    [JsonPropertyName("component")]
    public string Component { get; set; }

    [JsonPropertyName("majorVersion")]
    public int MajorVersion { get; set; }
}