using System;
using System.IO;

namespace Hyperlaunch.Instances;

public class Instance
{
    public string Name;
    public bool MultiVersion;
    public ClientType ClientType;
    public static Instance Default()
    {
        Instance res = new Instance();
        res.Name = "default";
        res.ClientType = ClientType.Vanilla;
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
}