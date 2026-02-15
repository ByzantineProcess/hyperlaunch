
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace Hyperlaunch.Download;

public class AssetManifest
{
    [JsonPropertyName("objects")]
    public Dictionary<string, Asset> Objects { get; set; }

    public string Original { get; private set; }
    public string Id { get; private set; }
    public static AssetManifest LoadFromJson(string json, string id)
    {
        var manifest = System.Text.Json.JsonSerializer.Deserialize<AssetManifest>(json);
        manifest.Original = json;
        manifest.Id = id;
        return manifest;
    }
    public static async Task<AssetManifest> LoadFromUrlAndVerifyAsync(string url, string sha1, string id)
    {
        string json = await Http.Client.GetStringAsync(url);
        if (!Sha1.Verify(json, sha1))
        {
            throw new System.Exception("Asset manifest failed integrity check.");
        }
        return LoadFromJson(json, id);
    }
    public static async Task<AssetManifest> LoadFromFileAndVerifyAsync(string path, string sha1, string id)
    {
        string json = await System.IO.File.ReadAllTextAsync(path);
        if (!Sha1.Verify(json, sha1))
        {
            throw new System.Exception("Asset manifest failed integrity check.");
        }
        return LoadFromJson(json, id);
    }
    public static async Task<AssetManifest> SmartLoadAsync(string url, string sha1, string id)
    {
        string cachePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "assets/", "indexes/", $"{id}.json");
        if (System.IO.File.Exists(cachePath))
        {
            try
            {
                return await LoadFromFileAndVerifyAsync(cachePath, sha1, id);
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Failed to load cached asset manifest for {id}: {ex.Message}. Will attempt to re-download.");
            }
        }
        var manifest = await LoadFromUrlAndVerifyAsync(url, sha1, id);
        manifest.SaveInCorrectSpot();
        return manifest;
    }
    
    public void SaveInCorrectSpot()
    {
        string assetsDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/", "assets/", "indexes/");
        System.IO.Directory.CreateDirectory(assetsDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(assetsDir, "objects.json"), Original);
    }
    
}

public class Asset
{
    [JsonPropertyName("hash")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}