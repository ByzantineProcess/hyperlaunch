using System.Collections.Generic;
using System.Text.Json.Serialization;

using Hyperlaunch.Download;
using Hyperlaunch.Settings;

namespace Hyperlaunch;

// all jsonable classes should go here
// need this to keep AOT compiler happy

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SettingsClass))]
[JsonSerializable(typeof(GameAccount))]
[JsonSerializable(typeof(Skin))]
[JsonSerializable(typeof(Cape))]
[JsonSerializable(typeof(AssetManifest))]
[JsonSerializable(typeof(Asset))]
[JsonSerializable(typeof(VersionManifestData))]
[JsonSerializable(typeof(Latest))]
[JsonSerializable(typeof(GameVersion))]
[JsonSerializable(typeof(ClientManifest))]
[JsonSerializable(typeof(Arguments))]
[JsonSerializable(typeof(ArgumentEntry))]
[JsonSerializable(typeof(ConditionalArgument))]
[JsonSerializable(typeof(Rule))]
[JsonSerializable(typeof(RuleFeatures))]
[JsonSerializable(typeof(RuleOs))]
[JsonSerializable(typeof(AssetIndex))]
[JsonSerializable(typeof(ClientDownloads))]
[JsonSerializable(typeof(DownloadInfo))]
[JsonSerializable(typeof(JavaVersionInfo))]
[JsonSerializable(typeof(Library))]
[JsonSerializable(typeof(LibraryDownloads))]
[JsonSerializable(typeof(LibraryArtifact))]
[JsonSerializable(typeof(ExtractRules))]
[JsonSerializable(typeof(LoggingConfig))]
[JsonSerializable(typeof(LoggingClient))]
[JsonSerializable(typeof(LoggingFile))]
[JsonSerializable(typeof(XboxLiveAuthRequest))]
[JsonSerializable(typeof(XboxLiveAuthProperties))]
[JsonSerializable(typeof(XstsAuthRequest))]
[JsonSerializable(typeof(XstsAuthProperties))]
[JsonSerializable(typeof(MinecraftLoginRequest))]
public partial class HyperlaunchJsonContext : JsonSerializerContext
{
}