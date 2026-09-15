using System.Collections.Generic;
using System.Text.Json.Serialization;

using Hyperlaunch.Download;
using Hyperlaunch.Instances.Loaders;
using Hyperlaunch.Instances.Mods.Modrinth;
using Hyperlaunch.Settings;
using Hyperlaunch.Utilities;

namespace Hyperlaunch;

// all json-ish classes should go here

// if you're back here again figuring out why some json class isn't working, look for an absence of {get; set;} thingies

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
[JsonSerializable(typeof(VersionRange))]
[JsonSerializable(typeof(FabricVersion))]
[JsonSerializable(typeof(Intermediary))]
[JsonSerializable(typeof(Loader))]
[JsonSerializable(typeof(List<FabricVersion>))]
[JsonSerializable(typeof(MiniProject))]
[JsonSerializable(typeof(Image))]
[JsonSerializable(typeof(Licence))]
[JsonSerializable(typeof(DonationPlatform))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(NeoForgeInstallProfile))]
[JsonSerializable(typeof(Dependency))]
[JsonSerializable(typeof(FullProjectBase))]
[JsonSerializable(typeof(V2Project))]
[JsonSerializable(typeof(FullProject))]
public partial class HyperlaunchJsonContext : JsonSerializerContext
{
}