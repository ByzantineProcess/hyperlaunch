using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hyperlaunch.Download;

public static class VersionManifest
{
    #nullable enable
    private static VersionManifestData? _data;
    #nullable disable

    public static VersionManifestData Data => _data ?? throw new InvalidOperationException("Version manifest has not been loaded.");

    public static async Task LoadAsync()
    {
        string cachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifest.tag");
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
            string cachedContent = await File.ReadAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifest.cache"));
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
                Log.Print("Etag from server: " + etag);
                await File.WriteAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifest.tag"), etag);
            }
            await File.WriteAllTextAsync(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifest.cache"), content);
        }

    }

    // function to get a GameVersion by its id (e.g. "1.20.1")
    public static GameVersion GetVersionById(string id)
    {
        // split id by : 
        string[] parts = id.Split(':');
        if (parts.Length == 2)
        {
            string baseVersionId = parts[0];
            string tag = parts[1];
            // get the base version
            GameVersion baseVersion = GetVersionById(baseVersionId);
            // Create a copy of the base version
            baseVersion = new GameVersion
            {
                Id = baseVersion.Id,
                Type = baseVersion.Type,
                ReleaseTime = baseVersion.ReleaseTime,
                Time = baseVersion.Time,
                Sha1 = baseVersion.Sha1,
                ComplianceLevel = baseVersion.ComplianceLevel
            };
            baseVersion.IsModded = true;
            baseVersion.BaseVersion = baseVersionId;
            baseVersion.Id = baseVersionId + "-" + tag;
            Log.Print($"Interpreted version id {baseVersion.Id} as modded version based on {baseVersionId} with tag {tag}");
            return baseVersion;
        }
        foreach (var version in Data.Versions)
        {
            if (version.Id == id)
            {
                return version;
            }
        }
        // print top 5 versions in manifest for debugging
        Log.Print("Available versions:");
        for (int i = 0; i < Math.Min(5, Data.Versions.Count); i++)
        {
            Log.Print(Data.Versions[i].Id);
        }
        throw new ArgumentException($"No version with id {id} found in manifest.");
        
    }
    // function to get the latest release version
    public static GameVersion GetLatestRelease()
    {
        return GetVersionById(Data.Latest.Release);
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
