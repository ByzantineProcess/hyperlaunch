using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace Hyperlaunch.Download;

public static class VersionManifest
{
    #nullable enable
    private static VersionManifestData? _data;
    #nullable disable

    public static VersionManifestData Data => _data ?? throw new InvalidOperationException("Version manifest has not been loaded.");

    public static async Task LoadAsync()
    {
        string cachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/", "manifest.tag");
        string cachedTag = null;
        if (File.Exists(cachePath))
        {
            cachedTag = File.ReadAllText(cachePath);
        }
        HttpRequestMessage etaggedRequest = new HttpRequestMessage(HttpMethod.Get, "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
        if (cachedTag != null)        
        {
            etaggedRequest.Headers.TryAddWithoutValidation("If-None-Match", cachedTag);
        }
        HttpResponseMessage response = await Http.Client.SendAsync(etaggedRequest);
        if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            string cachedContent = await File.ReadAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/", "manifest.cache"));
            _data = JsonSerializer.Deserialize<VersionManifestData>(cachedContent);
            return;
        }
        else
        {
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            _data = JsonSerializer.Deserialize<VersionManifestData>(content);
            if (response.Headers.TryGetValues("ETag", out var etagValues)) // response.Headers.ETag is null for some stupid reason
            {
                string etag = string.Join("", etagValues);
                GD.Print("Etag from server: " + etag);
                await File.WriteAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/", "manifest.tag"), etag);
            }
            await File.WriteAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/", "manifest.cache"), content);
        }

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
