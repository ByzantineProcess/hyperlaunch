#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperlaunch;
using Hyperlaunch.Download;
using Hyperlaunch.Instances;
using Hyperlaunch.Instances.Loaders;
using Hyperlaunch.Launch;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return;
        }
        bool forceModernJava = false;
        if (args.Contains("--force-modern-java"))
        {
            forceModernJava = true;
        }

        string command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "launch":
                await HandleLaunch(args, forceModernJava);
                break;

            case "login":
                await HandleLogin();
                break;

            case "versions":
                await HandleListVersions();
                break;
            
            case "launch-fabric":
                await Init();
                await VersionManifest.LoadAsync();
                ClientManifest fabricManifest = await Fabric.GetClientManifestWithStableFabricAsync(args[1], true);
                await HandleLaunch([], false, fabricManifest);
                break;
            
            case "checky":
                string path = CacheEverything.CalculateCachePath("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json", true);
                await CacheEverything.SmartGet("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
                Log.Print($"that cache path would look like {path}");
                break;

            default:
                Log.PrintErr($"Unknown command: {args[0]}");
                PrintUsage();
                Environment.Exit(1);
                break;
        }
        
    }

    static void PrintUsage()
    {
        Log.Print("hyperlaunch — fastest minecraft launcher around");
        Log.Print(" ");
        Log.Print("Usage:");
        Log.Print("  hyperlaunch-cli login                  Log in with your Microsoft account");
        Log.Print("  hyperlaunch-cli launch [version]       Download & launch a version (default: latest release)");
        Log.Print("  hyperlaunch-cli versions               List available versions");
        Log.Print("  hyperlaunch-cli launch-fabric          Download & launch a Fabric modded version");
    }

    static async Task Init()
    {
        string appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(appData,  ".hyperlaunch/"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(localApp, ".hyperlaunch/"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(localApp, ".hyperlaunch/", "instances/"));

        await Hyperlaunch.Settings.SettingsContainer.LoadAsync(
            System.IO.Path.Combine(appData, ".hyperlaunch/settings.json"));
    }

    static async Task HandleLogin()
    {
        string msToken = await MSAuth.Login();
        GameAccount account = await GameAccount.CreateAsync(msToken);
        Log.Print($"Logged in as {account.MinecraftUsername}");
    }

    static async Task HandleListVersions()
    {
        await Init();
        Log.Print($"Latest release:  {VersionManifest.Data.Latest.Release}");
        Log.Print($"Latest snapshot: {VersionManifest.Data.Latest.Snapshot}");
        Log.Print(" ");
        Log.Print("Recent versions:");
        int count = Math.Min(25, VersionManifest.Data.Versions.Count);
        for (int i = 0; i < count; i++)
        {
            var v = VersionManifest.Data.Versions[i];
            Log.Print($"  {v.Id,-20} [{v.Type}]");
        }
    }

    static async Task HandleLaunch(string[] args, bool forceModernJava, ClientManifest? overrideClientManifest = null)
    {
        Log.Print("starting launch");
        await Init();
        Log.Print("here");

        if (overrideClientManifest is not null)
        {
            await LaunchMinecraft.AuthAndLaunch(overrideClientManifest, Instance.Default());
        }
        else
        {
            string versionId = args[1];
            await LaunchMinecraft.AuthAndLaunch(versionId, Instance.Default());
        }
    }
}
