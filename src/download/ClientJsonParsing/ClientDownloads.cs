using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class ClientDownloads
{
    [JsonPropertyName("client")]
    public DownloadInfo Client { get; set; }

    #nullable enable
    [JsonPropertyName("client_mappings")]
    public DownloadInfo? ClientMappings { get; set; }

    [JsonPropertyName("server")]
    public DownloadInfo? Server { get; set; }

    [JsonPropertyName("server_mappings")]
    public DownloadInfo? ServerMappings { get; set; }

    [JsonPropertyName("windows_server")]
    public DownloadInfo? WindowsServer { get; set; }
    #nullable disable
}