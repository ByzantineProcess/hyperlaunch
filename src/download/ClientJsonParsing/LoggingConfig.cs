using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class LoggingConfig
{
    [JsonPropertyName("client")]
    public LoggingClient Client { get; set; }
}