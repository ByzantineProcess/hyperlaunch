

// recommended way to get forge-like version data is just by extracting it??? 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hyperlaunch.Download;
using Hyperlaunch.Utilities;
using Hyperlaunch.Utilities.JarTools;

namespace Hyperlaunch.Instances.Loaders;

#nullable enable

public static class NeoForge
{
    public const string BASE_URL = "https://maven.neoforged.net/";

    public static async Task<List<string>> GetValidNeoForgeVersionsAsync(string gameVersion)
    {
        // the JSON API provided by the maven server isn't set up to provide cache info, so probably best to use the xml even
        // if it means i get the privilege and joy and pure happiness and the desire to live of attempting to parse xml
        // update: it wasn't bad, thank you microslop. 
        string nfVersionsXml = await Cache.SmartGetString($"{BASE_URL}releases/net/neoforged/neoforge/maven-metadata.xml");
        // https://stackoverflow.com/a/7837848
        using TextReader nfxmlText = new StringReader(nfVersionsXml);

        XElement nfVersions = XElement.Load(nfxmlText, LoadOptions.None);
        List<XElement> versions = nfVersions.Descendants("version").ToList();

        if (gameVersion.StartsWith("1.")) {gameVersion = gameVersion.Remove(0, 2);}

        List<string> relevantNeoforgeVersions = versions.Select(version => version.Value).Where(version => version.StartsWith(gameVersion)).ToList();
        relevantNeoforgeVersions.Reverse(); // reorder to latest first

        return relevantNeoforgeVersions;
    }

    public static async Task<string> GetLatestNeoForgeVersionAsync(string gameVersion)
    {
        return (await GetValidNeoForgeVersionsAsync(gameVersion))[0];
    }

    public static async Task<ClientManifest> GetClientManifestAsync(string neoforgeVersion)
    {
        byte[] manifestBytes = await ZipCache.ReadFileFromZipUrl(
            $"{BASE_URL}releases/net/neoforged/neoforge/{neoforgeVersion}/neoforge-{neoforgeVersion}-installer.jar",
            "version.json",
            true
        );
        string manifestString = Encoding.UTF8.GetString(manifestBytes);
        return await ClientManifest.Parse(manifestString);
    }

    public static string? GetNeoFormVersionFromManifest(ClientManifest manifest)
    {
        foreach (ArgumentEntry argument in manifest.Arguments.Game)
        {
            if (argument.PlainValue == "--fml.neoFormVersion")
            {
                return manifest.Arguments.Game[manifest.Arguments.Game.LastIndexOf(argument)+1].PlainValue;
            }
        }
        return null;
    }

    public static async Task<ClientManifest> GetClientManifestForGameVersionAsync(string gameVersion)
    {
        return await GetClientManifestAsync(await GetLatestNeoForgeVersionAsync(gameVersion));
    }

    public static async Task InstallLoader(ClientManifest manifest, string neoforgeVersion)
    {
        string neoFormVersion = GetNeoFormVersionFromManifest(manifest)!;

        byte[] clientBytes = await ZipCache.ReadFileFromZipUrl(
            $"{BASE_URL}releases/net/neoforged/neoforge/{neoforgeVersion}/neoforge-{neoforgeVersion}-installer.jar",
            "data/client.lzma",
            false
        );
        byte[] iProfileBytes = await ZipCache.ReadFileFromZipUrl(
            $"{BASE_URL}releases/net/neoforged/neoforge/{neoforgeVersion}/neoforge-{neoforgeVersion}-installer.jar",
            "install_profile.json",
            true
        );
        NeoForgeInstallProfile profile = JsonSerializer.Deserialize(Encoding.UTF8.GetString(iProfileBytes), HyperlaunchJsonContext.Default.NeoForgeInstallProfile) ?? throw new Exception("bad");
        if (Encoding.UTF8.GetString(iProfileBytes).Contains("MOJMAP"))
        {
            throw new NotImplementedException("Obfuscated (Neo)Forge support is not finished.");
        }

        Patching.PatchJarWithNeoForgeBundle($"{DownloadTask.BaseVersionPath}/{manifest.BaseVersion}.jar", clientBytes, neoforgeVersion);


        await DownloadTask.ExecuteAllAsync(DownloadTask.FromLibraries(profile.Libraries));
    }
}