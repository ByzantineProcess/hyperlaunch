using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hyperlaunch.Download;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances;

public class Instance
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("multiversion")]
    public bool MultiVersion { get; set; }

    [JsonPropertyName("client_type")]
    public ClientType ClientType { get; set; }

    [JsonPropertyName("main_version")]
    public string MainVersion { get; set; }

    public Instance()
    {
    }

    public Instance(string name, ClientType clientType, string mainVersion, bool multiVersion)
    {
        Name = name;
        ClientType = clientType;
        MainVersion = mainVersion;
        MultiVersion = multiVersion;
    }

    public static Instance Default()
    {
        Instance res = new Instance();
        res.Name = "default";
        res.ClientType = ClientType.Vanilla;
        res.MultiVersion = true;

        return res;
    }
    public static Instance DefaultFabric()
    {
        Instance res = new Instance();
        res.Name = "default-fabric";
        res.ClientType = ClientType.Fabric;
        res.MainVersion = VersionManifest.GetLatestRelease().Id;
        res.MultiVersion = true;
        return res;
    }

    public string GetInstancePath()
    {
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string res = Path.Combine(localApp, ".hyperlaunch/", "instances/", Name);
        Directory.CreateDirectory(res);
        return res;
    }
    public static string GetInstancePath(string name)
    {
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string res = Path.Combine(localApp, ".hyperlaunch/", "instances/", name);
        Directory.CreateDirectory(res);
        return res;
    }

    public static Instance Load(string name)
    {
        string path = GetInstancePath(name);
        string jsonInstance = File.ReadAllText(Path.Combine(path, "hyperlaunch_instance.json"));
        Instance loaded = JsonSerializer.Deserialize(jsonInstance, HyperlaunchJsonContext.Default.Instance);
        return loaded;
    }

    public void Save()
    {
        string jsonInstance = JsonSerializer.Serialize(this, HyperlaunchJsonContext.Default.Instance);
        File.WriteAllText(Path.Combine(GetInstancePath(), "hyperlaunch_instance.json"), jsonInstance);
    }


    public static List<string> ListAll()
    {
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string ipath = Path.Combine(localApp, ".hyperlaunch/", "instances/");
        List<string> inames = Directory.GetDirectories(ipath).Select(path => path.Replace(ipath, "")).ToList();
        return inames;
    }
}