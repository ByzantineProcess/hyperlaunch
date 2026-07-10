using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class LoggingClient
{
    [JsonPropertyName("argument")]
    public string Argument { get; set; }

    [JsonPropertyName("file")]
    public LoggingFile File { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}