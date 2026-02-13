using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hyperlaunch.Download;

public static class VersionManifest
{
    #nullable enable
    private static VersionManifestData? _data;
    #nullable disable

    public static VersionManifestData Data => _data ?? throw new InvalidOperationException("Version manifest has not been loaded. Call LoadAsync() first.");

    public static async Task LoadAsync()
    {
        HttpResponseMessage response = await Http.Client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        _data = JsonSerializer.Deserialize<VersionManifestData>(content);
    }
}

public class VersionManifestData
{
    [JsonPropertyName("latest")]
    public Latest Latest { get; set; }

    [JsonPropertyName("versions")]
    public List<GameVersion> Versions { get; set; }
}

public class Latest
{
    [JsonPropertyName("release")]
    public string Release { get; set; }

    [JsonPropertyName("snapshot")]
    public string Snapshot { get; set; }
}