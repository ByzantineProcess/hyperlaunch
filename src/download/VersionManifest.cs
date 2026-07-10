using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Download;

public static class VersionManifest
{
    #nullable enable
    private static VersionManifestData? _data;
    #nullable disable

    public static VersionManifestData Data => _data ?? throw new InvalidOperationException("Version manifest has not been loaded.");

    public static async Task LoadAsync()
    {
        _data = JsonSerializer.Deserialize(await CacheEverything.SmartGetString("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json"), HyperlaunchJsonContext.Default.VersionManifestData);
        return;
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
