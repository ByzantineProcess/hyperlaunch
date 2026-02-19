using Hyperlaunch;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class GameVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("time")]
    public string Time { get; set; } = "";
    
    [JsonPropertyName("releaseTime")]
    public string ReleaseTime { get; set; } = "";
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
    
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";
    
    [JsonPropertyName("complianceLevel")]
    public int ComplianceLevel { get; set; } = 0;
    public bool IsModded = false;
    public string BaseVersion { get; set; } = "";
}