using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class Library
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    #nullable enable
    [JsonPropertyName("downloads")]
    public LibraryDownloads? Downloads { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }

    [JsonPropertyName("extract")]
    public ExtractRules? Extract { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }

    [JsonPropertyName("checksums")]
    public List<string>? Checksums { get; set; }

    [JsonPropertyName("serverreq")]
    public bool? ServerRequired { get; set; }

    [JsonPropertyName("clientreq")]
    public bool? ClientRequired { get; set; }
    #nullable disable

    public string GetExpectedPath()
    {
        var parts = Name.Split(':');
        if (parts.Length < 3)
            throw new FormatException($"Invalid library name format: {Name}");
        var groupId = parts[0];
        var artifactId = parts[1];
        var version = parts[2];
        var groupPath = groupId.Replace('.', '/');
        if (Name.Contains("net.minecraftforge:forge"))
        {
            return $"{groupPath}/{artifactId}/{version}/{artifactId}-{version}-universal.jar";
        }
        return $"{groupPath}/{artifactId}/{version}/{artifactId}-{version}.jar";
    }

    public string GetLibraryNameWithoutVersion()
    {
        string[] parts = Name.Split(':');
        if (parts.Length < 3)
            throw new FormatException($"Invalid library name format: {Name}");
        string groupId = parts[0];
        string artifactId = parts[1];
        return $"{groupId}:{artifactId}";

    }

    public string GetNativeClassifierKey(OsInfo os)
    {
        if (Natives == null) return null;
        if (!Natives.TryGetValue(os.Name, out var key)) return null;
        return key.Replace("${arch}", os.IntArch);
    }

    #nullable enable
    public LibraryArtifact? GetNativeArtifact(OsInfo os)
    {
        var key = GetNativeClassifierKey(os);
        if (key == null) return null;
        if (Downloads?.Classifiers == null) return null;
        Downloads.Classifiers.TryGetValue(key, out var artifact);
        return artifact;
    }

    public string GetMavenDownloadUrl()
    {
        string baseUrl = Url ?? "https://libraries.minecraft.net/";
        if (baseUrl == "https://libraries.minecraft.net/")
            Log.Print($"Warning: library {Name} has no URL specified, defaulting to {baseUrl}. This is probably not intended, and will likely fail to launch.");
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        Log.Print($"download URL for library {Name} should be {baseUrl + GetExpectedPath()}");
        return baseUrl + GetExpectedPath();
    }
}