using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Hyperlaunch.Download;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances.Loaders;

public static class Fabric
{
    public const string BASE_URL = "https://meta.fabricmc.net/v2/versions/";

    public static async Task<ClientManifest> GetClientManifestWithFabricAsync(string gameVersion, string loaderVersion, bool useCache = false)
    {
        string url = $"{BASE_URL}loader/{gameVersion}/{loaderVersion}/profile/json";
        return await ClientManifest.LoadFromUrlAsync(url, useCache);
    }

    public static async Task<FabricVersion> GetStableFabricVersionAsync(string gameVersion)
    {
        string url = $"{BASE_URL}loader/{gameVersion}/";
        Log.Print($"going to {url}...");
        string res = await CacheEverything.SmartGetString(url);
        List<FabricVersion> versionList = JsonSerializer.Deserialize(res, HyperlaunchJsonContext.Default.ListFabricVersion);
        // fabric meta should 400 instead of returning 0 but this is probably best
        if (versionList.Count == 0)
        {
            // TODO: should probably move exceptions to their own types at some point, not now though
            throw new Exception("No Fabric loader available for selected game version.");
        }
        // now we cycle through each one til we find a stable one
        foreach (FabricVersion version in versionList)
        {
            if (version.Loader.Stable)
            {
                return version;
            }
        }
        // couldn't find anything? just take the first one, it's probably good enough
        return versionList[0];
    }

    public static async Task<ClientManifest> GetClientManifestWithStableFabricAsync(string gameVersion, bool useCache = false)
    {
        FabricVersion fabricVersion = await GetStableFabricVersionAsync(gameVersion);
        return await GetClientManifestWithFabricAsync(gameVersion, fabricVersion.Loader.Version, useCache);
    }
}