// hey it's the code writer here
// i sat looking at the schema for like 4 hours
// then just gave up and told claude to do it
// idk if it works ngl

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

    public string Original { get; private set; }

    // ── Loading ─────────────────────────────────────────────────────────

    public static async Task<ClientManifest> LoadFromFileAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        ClientManifest res = JsonSerializer.Deserialize<ClientManifest>(json);
        res.Original = json;
        await res.AssetIndex.LoadIndexAsync();
        return res;
    }

    public static async Task<ClientManifest> LoadFromUrlAsync(string url)
    {
        string json = await Http.Client.GetStringAsync(url);
        ClientManifest res = JsonSerializer.Deserialize<ClientManifest>(json);
        res.Original = json;
        await res.AssetIndex.LoadIndexAsync();
        return res;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Returns flattened game arguments, evaluating rules against the supplied features.</summary>
    public List<string> ResolveGameArguments(FeatureSet features)
    {
        // Pre-1.13 format: single space-separated string, no rules
        if (Arguments == null && MinecraftArguments != null)
            return MinecraftArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        return ResolveArgumentList(Arguments?.Game, features, os: null);
    }

    /// <summary>Returns flattened JVM arguments, evaluating rules against the supplied OS info.</summary>
    public List<string> ResolveJvmArguments(OsInfo os)
    {
        // Pre-1.13 format: no JVM args in manifest, use sensible defaults
        if (Arguments == null || Arguments.Jvm == null)
        {
            return new List<string>
            {
                "-Djava.library.path=${natives_directory}",
                "-cp",
                "${classpath}"
            };
        }

        return ResolveArgumentList(Arguments.Jvm, features: null, os);
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
        // libraries should already be resolved by the time we call this
        // but we can still get a classpath 
        string classpath = BuildClasspath(os, DownloadTask.BaseLibraryPath, $"{DownloadTask.BaseVersionPath}/{Id}.jar");
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

// ── Arguments ───────────────────────────────────────────────────────────

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
    [JsonPropertyName("downloads")]
    public LibraryDownloads Downloads { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    #nullable enable
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }

    [JsonPropertyName("extract")]
    public ExtractRules? Extract { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }
    #nullable disable

    /// <summary>Parses the maven-style name into groupId, artifactId, version.</summary>
    public (string GroupId, string ArtifactId, string Version) ParseName()
    {
        var parts = Name.Split(':');
        if (parts.Length < 3)
            throw new FormatException($"Invalid library name format: {Name}");
        return (parts[0], parts[1], parts[2]);
    }

    /// <summary>Returns the expected path for this library relative to the libraries directory.</summary>
    public string GetExpectedPath()
    {
        var (groupId, artifactId, version) = ParseName();
        var groupPath = groupId.Replace('.', '/');
        return $"{groupPath}/{artifactId}/{version}/{artifactId}-{version}.jar";
    }

    /// <summary>Gets the native classifier key for the given OS, or null if no natives exist for this OS.</summary>
    public string GetNativeClassifierKey(OsInfo os)
    {
        if (Natives == null) return null;
        if (!Natives.TryGetValue(os.Name, out var key)) return null;
        return key.Replace("${arch}", os.Arch);
    }

    /// <summary>Gets the native classifier artifact for the given OS, or null if unavailable.</summary>
    public LibraryArtifact GetNativeArtifact(OsInfo os)
    {
        var key = GetNativeClassifierKey(os);
        if (key == null) return null;
        if (Downloads?.Classifiers == null) return null;
        Downloads.Classifiers.TryGetValue(key, out var artifact);
        return artifact;
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