
using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class VersionFile
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("hashes")]
    public required VersionFileHashes Hashes { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("primary")]
    public required bool Primary { get; set; }

    [JsonPropertyName("size")]
    public required int Size { get; set; }

    [JsonPropertyName("file_type")] // null except for resource packs?
    public string? FileType { get; set; }
}

public class VersionFileHashes
{
    [JsonPropertyName("sha1")]
    public required string Sha1 { get; set; }

    [JsonPropertyName("sha512")]
    public required string Sha512 { get; set; }
}