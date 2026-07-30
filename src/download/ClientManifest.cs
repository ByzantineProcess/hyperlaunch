// hey it's the code writer here
// i sat looking at the schema for like 4 hours
// then just gave up and told claude to do it
// i have looked over it and it should all be fine

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hyperlaunch.Instances;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Download;

// ── Root ────────────────────────────────────────────────────────────────

public class ClientManifest
{
    [JsonPropertyName("arguments")]
    public Arguments Arguments { get; set; }

    [JsonPropertyName("assetIndex")]
    public AssetIndex AssetIndex { get; set; }

    [JsonPropertyName("assets")]
    public string AssetCollection { get; set; }

    [JsonPropertyName("complianceLevel")]
    public int ComplianceLevel { get; set; }

    [JsonPropertyName("downloads")]
    public ClientDownloads Downloads { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("javaVersion")]
    public JavaVersionInfo JavaVersion { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; }

    #nullable enable
    [JsonPropertyName("logging")]
    public LoggingConfig? Logging { get; set; }
    #nullable disable

    #nullable enable
    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }
    #nullable disable

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; }

    [JsonPropertyName("minimumLauncherVersion")]
    public double MinimumLauncherVersion { get; set; }

    [JsonPropertyName("releaseTime")]
    public string ReleaseTime { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    #nullable enable
    /// <summary>If set, this manifest inherits missing fields from the specified parent version (e.g. Forge inheriting from vanilla).</summary>
    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }

    /// <summary>If set, the client jar to use on the classpath comes from this version instead of Id (e.g. Forge reusing the vanilla jar).</summary>
    [JsonPropertyName("jar")]
    public string? JarVersion { get; set; }
    #nullable disable

    public string Original { get; private set; }
    public bool IsModded { get; set; } = false;
    public string BaseVersion { get; set; } = "";

    // ── Inheritance ──────────────────────────────────────────────────────

    /// <summary>Merges this (child/modded) manifest onto a parent manifest, inheriting any fields that are null in the child.</summary>
    public void MergeWithParent(ClientManifest parent)
    {
        // Merge argument lists: concatenate parent entries after child's so both sets are used.
        if (parent.Arguments != null)
        {
            Arguments ??= new Arguments();
            if (parent.Arguments.Game != null)
            {
                Arguments.Game ??= new List<ArgumentEntry>();
                Arguments.Game.AddRange(parent.Arguments.Game);
            }
            if (parent.Arguments.Jvm != null)
            {
                Arguments.Jvm ??= new List<ArgumentEntry>();
                Arguments.Jvm.AddRange(parent.Arguments.Jvm);
            }
            if (parent.Arguments.DefaultJvm != null)
            {
                Arguments.DefaultJvm ??= new List<ArgumentEntry>();
                Arguments.DefaultJvm.AddRange(parent.Arguments.DefaultJvm);
            }
        }

        AssetIndex         ??= parent.AssetIndex;
        AssetCollection    ??= parent.AssetCollection;
        Downloads          ??= parent.Downloads;
        JavaVersion        ??= parent.JavaVersion;
        Logging            ??= parent.Logging;
        MinecraftArguments ??= parent.MinecraftArguments;
        MainClass          ??= parent.MainClass;
        JarVersion         ??= parent.JarVersion;

        // Concatenate libraries: child's first (higher priority), then parent's.
        var merged = new List<Library>();
        merged.AddRange(Libraries ?? new List<Library>());
        merged.AddRange(parent.Libraries ?? new List<Library>());
        Libraries = merged;

        // Inherit numeric fields only when the child left them at default.
        if (ComplianceLevel == 0) ComplianceLevel = parent.ComplianceLevel;
        if (MinimumLauncherVersion == 0) MinimumLauncherVersion = parent.MinimumLauncherVersion;
    }

    // ── Loading ─────────────────────────────────────────────────────────

    public static async Task<ClientManifest> LoadFromFileAsync(string path, bool modded = false, string baseVersion = "", string moddedId = "")
    {
        string json = await File.ReadAllTextAsync(path);
        ClientManifest res = JsonSerializer.Deserialize(json, HyperlaunchJsonContext.Default.ClientManifest);
        res.Original = json;

        // Handle inheritsFrom: load the parent manifest and merge inherited fields.
        if (res.InheritsFrom != null)
        {
            Log.Print($"Manifest inherits from {res.InheritsFrom}, loading parent...");
            var parentVersion = VersionManifest.GetVersionById(res.InheritsFrom);
            var parent = await LoadFromVersionWithCacheAsync(parentVersion);
            res.MergeWithParent(parent);
        }

        // Load asset index (may already be populated if inherited from a loaded parent).
        if (res.AssetIndex != null && res.AssetIndex.Index == null)
        {
            await res.AssetIndex.LoadIndexAsync();
        }

        if (modded)
        {
            Log.Print("Version is modded, marking manifest as modded and setting base version");
            res.IsModded = true;
            res.BaseVersion = baseVersion;
            res.Id = moddedId;
        }
        
        return res;
    }

    public static async Task<ClientManifest> LoadFromVersionAsync(GameVersion version)
    {
        string json = await Cache.SmartGetString(version.Url);
        ClientManifest res = JsonSerializer.Deserialize(json, HyperlaunchJsonContext.Default.ClientManifest);
        res.Original = json;

        // Handle inheritsFrom (unlikely from a URL, but supported for completeness).
        if (res.InheritsFrom != null)
        {
            Log.Print($"Manifest inherits from {res.InheritsFrom}, loading parent...");
            var parentVersion = VersionManifest.GetVersionById(res.InheritsFrom);
            var parent = await LoadFromVersionWithCacheAsync(parentVersion);
            res.MergeWithParent(parent);
        }

        if (res.AssetIndex != null && res.AssetIndex.Index == null)
        {
            await res.AssetIndex.LoadIndexAsync();
        }

        return res;
    }

    public static async Task<ClientManifest> LoadFromUrlAsync(string url, bool useCacheAsMuchAsPossible = false)
    {
        string json = await Cache.SmartGetString(url, useCacheAsMuchAsPossible);
        ClientManifest res = JsonSerializer.Deserialize(json, HyperlaunchJsonContext.Default.ClientManifest);
        res.Original = json;

        if (res.InheritsFrom != null)
        {
            Log.Print($"Manifest inherits from {res.InheritsFrom}, loading parent...");
            var parentVersion = VersionManifest.GetVersionById(res.InheritsFrom);
            var parent = await LoadFromVersionWithCacheAsync(parentVersion);
            res.MergeWithParent(parent);
        }

        if (res.AssetIndex != null && res.AssetIndex.Index == null)
        {
            await res.AssetIndex.LoadIndexAsync();
        }

        return res;
    }

    public static async Task<ClientManifest> LoadFromVersionWithCacheAsync(GameVersion version)
    {
        Log.Print($"Attempting to load client manifest for version {version.Id} from cache...");
        string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifests/");
        if (File.Exists(cachePath + $"{version.Id}.json"))
        {
            try
            {
                Log.Print($"Found cached manifest for version {version.Id}!");
                if (version.IsModded)
                {
                    return await LoadFromFileAsync(cachePath + $"{version.Id}.json", true, version.BaseVersion, version.Id);
                }
                return await LoadFromFileAsync(cachePath + $"{version.Id}.json");
            }
            catch (Exception ex)
            {
                Log.Print($"Failed to load client manifest from cache, will attempt to redownload. Error: {ex.Message}");
            }
        }
        Log.Print($"Cache miss for client manifest at {cachePath + $"{version.Id}.json"}, downloading from {version.Url}...");
        var manifest = await LoadFromVersionAsync(version);
        if (!Sha1.Verify(manifest.Original, version.Sha1))
        {
            throw new Exception("Downloaded client manifest failed integrity check.");
        }
        // ensure directory exists before saving
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
        await File.WriteAllTextAsync(cachePath + $"{version.Id}.json", manifest.Original);
        if (version.IsModded)
        {
            Log.Print("Version is modded, marking manifest as modded and setting base version");
            manifest.IsModded = true;
            manifest.BaseVersion = version.BaseVersion;
            manifest.Id = version.Id + "-" + version.BaseVersion;
        }
        return manifest;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Resolves logging argument for this version, returning null if logging is not configured or not supported on this version.</summary>
    public string ResolveLoggingArgument(OsInfo os)
    {
        if (Logging == null || Logging.Client == null) return null;
        // log xml files live in localappdata/.hyperlaunch/configs/logging/ID.json
        string arg = Logging.Client.Argument;
        arg = arg.Replace("${path}", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "configs/logging/", $"{Id}.json"));
        return arg;
    }

    /// <summary>Returns flattened game arguments, evaluating rules against the supplied features.</summary>
    public List<string> ResolveGameArguments(FeatureSet features)
    {
        // Pre-1.13 format: single space-separated string, no rules
        if (MinecraftArguments != null)
        {
            return MinecraftArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        return ResolveArgumentList(Arguments?.Game, features, os: OsInfo.Detect());
    }

    /// <summary>Returns flattened JVM arguments, evaluating rules against the supplied OS info.</summary>
    public List<string> ResolveJvmArguments(OsInfo os)
    {
        // Pre-1.13 format: no JVM args in manifest, use sensible defaults
        if (Arguments == null || Arguments.Jvm.Count == 0)
        {
            return new List<string>
            {
                "-Djava.library.path=${natives_directory}",
                ResolveLoggingArgument(os),
                "-cp",
                "${classpath}"
            }.Where(a => a != null).ToList();
        }

        return ResolveArgumentList(Arguments.Jvm, features: null, os).Append(ResolveLoggingArgument(os)).Where(a => a != null).ToList();
    }

    public List<string> ResolveDefaultJvmArguments(OsInfo os)
    {
        if (Arguments == null || Arguments.DefaultJvm.Count == 0)
        {
            return new List<string>{};
        }

        return ResolveArgumentList(Arguments.DefaultJvm, features: null, os).Where(a => a != null).ToList();
    }

    /// <summary>Returns all libraries whose rules pass for the given OS.</summary>
    public List<Library> ResolveLibraries(OsInfo os)
    {
        var result = new List<Library>();
        foreach (var lib in Libraries)
        {
            if (RulesPass(lib.Rules, features: null, os))
                result.Add(lib);
        }
        return result;
    }

    /// <summary>Builds a classpath string from resolved libraries and the client jar.</summary>
    public string BuildClasspath(OsInfo os, string librariesDir, string clientJarPath)
    {
        var separator = os.Name == "windows" ? ";" : ":";
        var paths = new List<string>();
        foreach (var lib in ResolveLibraries(os))
        {
            if (lib.Downloads?.Artifact != null)
                paths.Add(Path.Combine(librariesDir, lib.Downloads.Artifact.Path.Replace('/', Path.DirectorySeparatorChar)));
            else if (lib.Name != null)
                // Maven-style library without explicit download info – derive path from coordinates.
                paths.Add(Path.Combine(librariesDir, lib.GetExpectedPath().Replace('/', Path.DirectorySeparatorChar)));
        }
        paths.Add(clientJarPath);
        return string.Join(separator, paths);
    }

    //<summary>Resolves all launch arguments at once. Returns a list of strings which are arguments to the JVM.\nResolved args are unformatted and may contain placeholders which need to be replaced by the caller.</summary>
    public List<string> ResolveLaunchCommand(GameAccount account, Instance instance, bool includeDefaultJvmArgs = false)
    {
        // first resolve JVM arguments with OS info since they may affect library selection
        OsInfo os = OsInfo.Detect();
        List<string> jvmArgs = ResolveJvmArguments(os);
        // libraries should already be resolved by the time we call this
        // but we can still get a classpath
        // Use the jar key if set (e.g. Forge reuses the vanilla jar)
        string jarVersion = JarVersion ?? Id;
        string classpath = BuildClasspath(os, DownloadTask.BaseLibraryPath, $"{DownloadTask.BaseVersionPath}/{jarVersion}.jar");
        // make a directory for dumped native libs to go in
        string nativesDir = Path.Combine(DownloadTask.BasePath, "natives/", Id);
        Directory.CreateDirectory(nativesDir);
        // TODO: find an authoritative list of placeholders, pretty sure i'm missing some
        jvmArgs = jvmArgs.Select(arg => arg
            .Replace("${launcher_name}", "Hyperlaunch")
            .Replace("${launcher_version}", "0.1")
            .Replace("${natives_directory}", nativesDir)
            .Replace("${classpath}", classpath))
            .ToList();
        // then get the game arguments
        List<string> gameArgs = ResolveGameArguments(new FeatureSet{});
        string basePath = instance.GetInstancePath();
        gameArgs = gameArgs.Select(arg => arg
            .Replace("${auth_player_name}", account.MinecraftUsername)
            .Replace("${version_name}", Id)
            .Replace("${auth_uuid}", account.MinecraftUUID)
            .Replace("${auth_access_token}", account.MinecraftAccessToken)
            .Replace("${game_directory}", basePath) // TODO: instancing goes here
            .Replace("${assets_root}", DownloadTask.BasePath + "assets/")
            .Replace("${assets_index_name}", AssetCollection)
            .Replace("${version_type}", Type)
            .Replace("${launcher_name}", "Hyperlaunch")
            .Replace("${launcher_version}", "0.1")
            .Replace("${natives_directory}", nativesDir)
            .Replace("${user_type}", "msa"))
            .ToList();
        // truthfully i have no idea what an xuid is
        gameArgs.Remove("${auth_xuid}");
        gameArgs.Remove("--xuid");
        // remove telemetry (?)
        gameArgs.Remove("${clientid}");
        gameArgs.Remove("--clientId");
        // the wiki has no idea what this does so just yeet it
        gameArgs.Remove("${user_properties}");
        gameArgs.Remove("--userProperties");


        if (Arguments is not null && Arguments.DefaultJvm is not null)
        {
            List<string> defaultJvmArgs = ResolveDefaultJvmArguments(os);
            jvmArgs = jvmArgs.Concat(defaultJvmArgs).ToList();
        }

        // build final command: JVM args + main class + game args
        return jvmArgs.Append(MainClass).Concat(gameArgs).ToList();
    }

    // ── Rule evaluation ─────────────────────────────────────────────────

    private static List<string> ResolveArgumentList(List<ArgumentEntry> entries, FeatureSet features, OsInfo os)
    {
        var result = new List<string>();
        if (entries == null) return result;
        foreach (var entry in entries)
        {
            if (!entry.IsConditional)
            {
                result.Add(entry.PlainValue);
            }
            else if (RulesPass(entry.Conditional.Rules, features, os))
            {
                result.AddRange(entry.Conditional.Value);
            }
        }
        return result;
    }

    private static bool RulesPass(List<Rule> rules, FeatureSet features, OsInfo os)
    {
        if (rules == null || rules.Count == 0) return true;

        bool allowed = false;
        foreach (var rule in rules)
        {
            bool matches = true;

            if (rule.Os != null && os != null)
            {
                if (rule.Os.Name != null && !string.Equals(rule.Os.Name, os.Name, StringComparison.OrdinalIgnoreCase))
                    matches = false;
                if (rule.Os.Arch != null && !string.Equals(rule.Os.Arch, os.Arch, StringComparison.OrdinalIgnoreCase))
                    matches = false;
                if (rule.Os.Version != null && os.Version != null)
                {
                    if (!Regex.IsMatch(os.Version.ToString(), rule.Os.Version))
                        matches = false;
                }
                if (rule.Os.VersionRange != null && os.Version != null)
                {
                    if (!SemanticVersionParser.Parse(rule.Os.VersionRange, os.Version))
                        matches = false;
                    
                    // Console.WriteLine($"os version check: os version {os.Version} returned {SemanticVersionParser.Parse(rule.Os.VersionRange, os.Version)}");
                }
            }

            if (rule.Features != null && features != null)
            {
                if (rule.Features.IsDemoUser.HasValue && rule.Features.IsDemoUser.Value != features.IsDemoUser)
                    matches = false;
                if (rule.Features.HasCustomResolution.HasValue && rule.Features.HasCustomResolution.Value != features.HasCustomResolution)
                    matches = false;
                if (rule.Features.HasQuickPlaysSupport.HasValue && rule.Features.HasQuickPlaysSupport.Value != features.HasQuickPlaysSupport)
                    matches = false;
                if (rule.Features.IsQuickPlaySingleplayer.HasValue && rule.Features.IsQuickPlaySingleplayer.Value != features.IsQuickPlaySingleplayer)
                    matches = false;
                if (rule.Features.IsQuickPlayMultiplayer.HasValue && rule.Features.IsQuickPlayMultiplayer.Value != features.IsQuickPlayMultiplayer)
                    matches = false;
                if (rule.Features.IsQuickPlayRealms.HasValue && rule.Features.IsQuickPlayRealms.Value != features.IsQuickPlayRealms)
                    matches = false;
            }

            if (matches)
                allowed = rule.Action == "allow";
        }
        return allowed;
    }
}