// hey it's the code writer here
// i sat looking at the schema for like 4 hours
// then just gave up and told claude to do it
// i have looked over it and it should all be fine

// should i move every class to its own file? 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
    public int MinimumLauncherVersion { get; set; }

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
        ClientManifest res = JsonSerializer.Deserialize<ClientManifest>(json);
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
        string json = await Http.Client.GetStringAsync(version.Url);
        ClientManifest res = JsonSerializer.Deserialize<ClientManifest>(json);
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

    public static async Task<ClientManifest> LoadFromVersionWithCacheAsync(GameVersion version)
    {
        Log.Print($"Attempting to load client manifest for version {version.Id} from cache...");
        string cachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "manifests/");
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
        arg = arg.Replace("${path}", Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "configs/logging/", $"{Id}.json"));
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
    public List<string> ResolveLaunchCommand(GameAccount account)
    {
        // first resolve JVM arguments with OS info since they may affect library selection
        OsInfo os = OsInfo.Detect();
        List<string> jvmArgs = ResolveJvmArguments(os);
        Log.Print("Resolved JVM arguments:");
        foreach (var arg in jvmArgs)
        {
            Log.Print(arg);
        }
        // libraries should already be resolved by the time we call this
        // but we can still get a classpath
        // Use the jar key if set (e.g. Forge reuses the vanilla jar)
        string jarVersion = JarVersion ?? Id;
        string classpath = BuildClasspath(os, DownloadTask.BaseLibraryPath, $"{DownloadTask.BaseVersionPath}/{jarVersion}.jar");
        // make a directory for dumped native libs to go in
        string nativesDir = Path.Combine(DownloadTask.BasePath, "natives/", Id);
        Directory.CreateDirectory(nativesDir);
        jvmArgs = jvmArgs.Select(arg => arg
            .Replace("${launcher_name}", "Hyperlaunch")
            .Replace("${launcher_version}", "0.1")
            .Replace("${natives_directory}", nativesDir)
            .Replace("${classpath}", classpath))
            .ToList();
        // then get the game arguments
        List<string> gameArgs = ResolveGameArguments(new FeatureSet{});
        gameArgs = gameArgs.Select(arg => arg
            .Replace("${auth_player_name}", account.MinecraftUsername)
            .Replace("${version_name}", Id)
            .Replace("${auth_uuid}", account.MinecraftUUID)
            .Replace("${auth_access_token}", account.MinecraftAccessToken)
            .Replace("${game_directory}", DownloadTask.BasePath)
            .Replace("${assets_root}", DownloadTask.BasePath + "assets/")
            .Replace("${assets_index_name}", AssetCollection)
            .Replace("${version_type}", Type)
            .Replace("${launcher_name}", "Hyperlaunch")
            .Replace("${launcher_version}", "0.1")
            .Replace("${natives_directory}", nativesDir)
            .Replace("${user_type}", "msa"))
            .ToList();
        // remove xuid related placeholders since we don't have that info
        gameArgs.Remove("${auth_xuid}");
        gameArgs.Remove("--xuid");
        // remove telemetry (?)
        gameArgs.Remove("${clientid}");
        gameArgs.Remove("--clientId");
        // the wiki has no idea what this does so just yeet it
        gameArgs.Remove("${user_properties}");
        gameArgs.Remove("--userProperties");
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
                    if (!System.Text.RegularExpressions.Regex.IsMatch(os.Version, rule.Os.Version))
                        matches = false;
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


public class Arguments
{
    [JsonPropertyName("game")]
    public List<ArgumentEntry> Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<ArgumentEntry> Jvm { get; set; }
}

/// <summary>
/// Represents a single argument entry which is either a plain string
/// or a conditional argument with rules and a value.
/// </summary>
[JsonConverter(typeof(ArgumentEntryConverter))]
public class ArgumentEntry
{
    #nullable enable
    public string? PlainValue { get; set; }
    public ConditionalArgument? Conditional { get; set; }
    #nullable disable

    public bool IsConditional => Conditional != null;
}

public class ConditionalArgument
{
    [JsonPropertyName("rules")]
    public List<Rule> Rules { get; set; }

    /// <summary>Normalised to a list even when the JSON field is a single string.</summary>
    [JsonPropertyName("value")]
    [JsonConverter(typeof(StringOrStringListConverter))]
    public List<string> Value { get; set; }
}

// ── Rules ───────────────────────────────────────────────────────────────

public class Rule
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    #nullable enable
    [JsonPropertyName("features")]
    public RuleFeatures? Features { get; set; }

    [JsonPropertyName("os")]
    public RuleOs? Os { get; set; }
    #nullable disable
}

public class RuleFeatures
{
    [JsonPropertyName("is_demo_user")]
    public bool? IsDemoUser { get; set; }

    [JsonPropertyName("has_custom_resolution")]
    public bool? HasCustomResolution { get; set; }

    [JsonPropertyName("has_quick_plays_support")]
    public bool? HasQuickPlaysSupport { get; set; }

    [JsonPropertyName("is_quick_play_singleplayer")]
    public bool? IsQuickPlaySingleplayer { get; set; }

    [JsonPropertyName("is_quick_play_multiplayer")]
    public bool? IsQuickPlayMultiplayer { get; set; }

    [JsonPropertyName("is_quick_play_realms")]
    public bool? IsQuickPlayRealms { get; set; }
}

public class RuleOs
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    #nullable enable
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }
    #nullable disable
}

// ── Asset index ─────────────────────────────────────────────────────────

public class AssetIndex
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("totalSize")]
    public int TotalSize { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    public AssetManifest Index { get; private set; }

    public async Task LoadIndexAsync()
    {
        Index = await AssetManifest.SmartLoadAsync(Url, Sha1, Id);
    }
}

// ── Downloads ───────────────────────────────────────────────────────────

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

public class DownloadInfo
{
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

// ── Java version ────────────────────────────────────────────────────────

public class JavaVersionInfo
{
    [JsonPropertyName("component")]
    public string Component { get; set; }

    [JsonPropertyName("majorVersion")]
    public int MajorVersion { get; set; }
}

// ── Libraries ───────────────────────────────────────────────────────────

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

    /// <summary>Returns the expected path for this library relative to the libraries directory.</summary>
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
            // old forge versions are stupid and dumb and bad
            return $"{groupPath}/{artifactId}/{version}/{artifactId}-{version}-universal.jar";
        }
        return $"{groupPath}/{artifactId}/{version}/{artifactId}-{version}.jar";
    }

    /// <summary>Gets the native classifier key for the given OS, or null if no natives exist for this OS.</summary>
    public string GetNativeClassifierKey(OsInfo os)
    {
        if (Natives == null) return null;
        if (!Natives.TryGetValue(os.Name, out var key)) return null;
        return key.Replace("${arch}", os.IntArch);
    }

    /// <summary>Gets the native classifier artifact for the given OS, or null if unavailable.</summary>
    public LibraryArtifact GetNativeArtifact(OsInfo os)
    {
        Log.Print($"Attempting to get native artifact for library {Name} on OS {os.Name} {os.Arch}...");
        var key = GetNativeClassifierKey(os);
        Log.Print($"Native classifier key for OS {os.Name} is {key}");
        if (key == null) return null;
        if (Downloads?.Classifiers == null) return null;
        Downloads.Classifiers.TryGetValue(key, out var artifact);
        Log.Print(artifact != null
            ? $"Found native artifact for library {Name} with classifier {key}: {artifact.Url}"
            : $"No native artifact found for library {Name} with classifier {key}");
        return artifact;
    }

    /// <summary>
    /// Returns the download URL for this library by constructing it from the Maven
    /// coordinates (<see cref="Name"/>) and the optional repository base <see cref="Url"/>.
    /// Falls back to the default Minecraft libraries CDN when no URL is specified.
    /// </summary>
    public string GetMavenDownloadUrl()
    {
        string baseUrl = Url ?? "https://libraries.minecraft.net/";
        // warn if downloading from main URL since that likely means the manifest is missing download info and we're just guessing based on the name
        if (baseUrl == "https://libraries.minecraft.net/")
            Log.Print($"Warning: library {Name} has no URL specified, defaulting to {baseUrl}. This is probably not intended, and will likely fail to launch.");
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        Log.Print($"download URL for library {Name} should be {baseUrl + GetExpectedPath()}");
        return baseUrl + GetExpectedPath();
    }
}

public class LibraryDownloads
{
    #nullable enable
    [JsonPropertyName("artifact")]
    public LibraryArtifact? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, LibraryArtifact>? Classifiers { get; set; }
    #nullable disable
}

public class LibraryArtifact
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class ExtractRules
{
    [JsonPropertyName("exclude")]
    public List<string> Exclude { get; set; }
}

// ── Logging ─────────────────────────────────────────────────────────────

public class LoggingConfig
{
    [JsonPropertyName("client")]
    public LoggingClient Client { get; set; }
}

public class LoggingClient
{
    [JsonPropertyName("argument")]
    public string Argument { get; set; }

    [JsonPropertyName("file")]
    public LoggingFile File { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class LoggingFile
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

// ── Helper types for rule evaluation ────────────────────────────────────

/// <summary>Feature flags to check when resolving game arguments.</summary>
public class FeatureSet
{
    public bool IsDemoUser { get; set; }
    public bool HasCustomResolution { get; set; }
    public bool HasQuickPlaysSupport { get; set; }
    public bool IsQuickPlaySingleplayer { get; set; }
    public bool IsQuickPlayMultiplayer { get; set; }
    public bool IsQuickPlayRealms { get; set; }
}

/// <summary>Current OS information for JVM argument and library resolution.</summary>
public class OsInfo
{
    public string Name { get; set; }   // "windows", "osx", or "linux"
    public string Version { get; set; }
    public string Arch { get; set; }   // e.g. "x86", "x86_64"
    public string IntArch => Arch switch
    {
        "x86" => "32",
        "x86_64" => "64",
        "aarch64" => "64",
        _ => "64" // default to 64-bit if unknown, common enough nowadays
    };

    public static OsInfo Detect()
    {
        string name;
        if (OperatingSystem.IsWindows()) name = "windows";
        else if (OperatingSystem.IsMacOS()) name = "osx";
        else name = "linux";

        return new OsInfo
        {
            Name = name,
            Version = Environment.OSVersion.Version.ToString(),
            Arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.X86 => "x86",
                System.Runtime.InteropServices.Architecture.X64 => "x86_64",
                System.Runtime.InteropServices.Architecture.Arm64 => "aarch64",
                _ => "unknown"
            }
        };
    }
}

// ── JSON converters ─────────────────────────────────────────────────────

/// <summary>
/// Handles argument list entries which can be either a plain JSON string
/// or a JSON object with "rules" and "value" fields.
/// </summary>
public class ArgumentEntryConverter : JsonConverter<ArgumentEntry>
{
    public override ArgumentEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ArgumentEntry { PlainValue = reader.GetString() };
        }

        var conditional = JsonSerializer.Deserialize<ConditionalArgument>(ref reader, options);
        return new ArgumentEntry { Conditional = conditional };
    }

    public override void Write(Utf8JsonWriter writer, ArgumentEntry value, JsonSerializerOptions options)
    {
        if (!value.IsConditional)
            writer.WriteStringValue(value.PlainValue);
        else
            JsonSerializer.Serialize(writer, value.Conditional, options);
    }
}

/// <summary>
/// Handles the "value" field in conditional arguments which can be either
/// a single JSON string or an array of strings.
/// </summary>
public class StringOrStringListConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new List<string> { reader.GetString() };
        }

        var list = new List<string>();
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                list.Add(reader.GetString());
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
            writer.WriteStringValue(value[0]);
        else
            JsonSerializer.Serialize(writer, value, options);
    }
}