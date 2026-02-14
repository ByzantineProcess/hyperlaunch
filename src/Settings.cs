using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hyperlaunch.Settings;

// this class ai generated because no one knows about c# singletons online apparently
public static class SettingsContainer
{
    private static readonly object Sync = new();

    #nullable enable
    private static SettingsClass? _current;
    #nullable disable

    public static bool IsLoaded
    {
        get
        {
            lock (Sync)
            {
                return _current != null;
            }
        }
    }

    public static SettingsClass Current
    {
        get
        {
            lock (Sync)
            {
                return _current ?? throw new InvalidOperationException("Settings have not been loaded.");
            }
        }
    }

    public static async Task<SettingsClass> LoadAsync(string filepath)
    {
        var settings = await SettingsClass.LoadFromFileOrDefault(filepath);
        lock (Sync)
        {
            _current = settings;
        }

        return settings;
    }

    public static void Set(SettingsClass settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        lock (Sync)
        {
            _current = settings;
        }
    }

    public static async Task SaveAsync(string filepath)
    {
        SettingsClass settings;
        lock (Sync)
        {
            settings = _current ?? throw new InvalidOperationException("Settings have not been loaded.");
        }

        await settings.SaveToFile(filepath);
    }
}

public class SettingsClass
{
    public async static Task<SettingsClass> LoadFromFileOrDefault(string filepath)
    {
        // check if file exists
        if (!File.Exists(filepath))
        {
            return new SettingsClass();
        }
        string settingsProfileSerialised = await File.ReadAllTextAsync(filepath);
        return JsonSerializer.Deserialize<SettingsClass>(settingsProfileSerialised);
    }
    public async Task SaveToFile(string filepath)
    {
        string settingsProfileSerialised = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filepath, settingsProfileSerialised);
    }
    public string SaveToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
    public SettingsClass()
    {
        // load defaults i guess?
    }
    
    // settings fields go here

    public int SettingsVersion = 1;
    public bool OnlyUseRecommendedJavaVersions = false;
    public bool UseProxy = false;
    public string ProxyAddress = "";
    public bool UseAlternativeAppDataFolder = false;
    public string AlternativeAppDataFolder = "";
    public bool ForgetTokensOnExit = false;
    public List<string> KnownJVMs = [];

}

