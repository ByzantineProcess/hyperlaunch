using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperlaunch;
using Hyperlaunch.Download;
using Hyperlaunch.Launch;

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

        string command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "launch":
                await HandleLaunch(args);
                break;

            case "login":
                await HandleLogin();
                break;

            case "versions":
                await HandleListVersions();
                break;

            default:
                Console.Error.WriteLine($"Unknown command: {args[0]}");
                PrintUsage();
                Environment.Exit(1);
                break;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("hyperlaunch — fastest minecraft launcher around");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  hyperlaunch-cli login                  Log in with your Microsoft account");
        Console.WriteLine("  hyperlaunch-cli launch [version]       Download & launch a version (default: latest release)");
        Console.WriteLine("  hyperlaunch-cli versions               List available versions");
    }

    static async Task Init()
    {
        string appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(appData,  ".hyperlaunch/"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(localApp, ".hyperlaunch/"));

        await Hyperlaunch.Settings.SettingsContainer.LoadAsync(
            System.IO.Path.Combine(appData, ".hyperlaunch/settings.json"));
        await VersionManifest.LoadAsync();
    }

    static async Task HandleLogin()
    {
        string msToken = await MSAuth.Login();
        GameAccount account = await GameAccount.CreateAsync(msToken);
        Console.WriteLine($"Logged in as {account.MinecraftUsername}");
    }

    static async Task HandleListVersions()
    {
        await Init();
        Console.WriteLine($"Latest release:  {VersionManifest.Data.Latest.Release}");
        Console.WriteLine($"Latest snapshot: {VersionManifest.Data.Latest.Snapshot}");
        Console.WriteLine();
        Console.WriteLine("Recent versions:");
        int count = Math.Min(25, VersionManifest.Data.Versions.Count);
        for (int i = 0; i < count; i++)
        {
            var v = VersionManifest.Data.Versions[i];
            Console.WriteLine($"  {v.Id,-20} [{v.Type}]");
        }
    }

    static async Task HandleLaunch(string[] args)
    {
        await Init();

        // Login
        Console.WriteLine("Authenticating...");
        string msToken = await MSAuth.Login();
        GameAccount account = await GameAccount.CreateAsync(msToken);
        Console.WriteLine($"Logged in as {account.MinecraftUsername}");

        // Resolve version
        string versionId;
        if (args.Length >= 2)
        {
            versionId = args[1];
        }
        else
        {
            versionId = VersionManifest.Data.Latest.Release;
            Console.WriteLine($"No version specified, using latest release: {versionId}");
        }

        GameVersion version = VersionManifest.GetVersionById(versionId);

        // Download
        Console.WriteLine($"Loading client manifest for {versionId}...");
        ClientManifest clientManifest = await ClientManifest.LoadFromVersionAsync(version.Url);
        clientManifest.AssetIndex.Index.SaveInCorrectSpot();

        List<DownloadTask> downloadTasks = DownloadTask.FromClientJson(clientManifest);
        Console.WriteLine($"{downloadTasks.Count} files to download");

        var progress = new Progress<long>(bytesRemaining =>
        {
            double mbRemaining = bytesRemaining / (1024.0 * 1024.0);
            Console.Write($"\rDownload progress: {mbRemaining:F2} MB remaining   ");
        });

        await DownloadTask.ExecuteAllAsync(downloadTasks, maxConcurrentNetwork: 50, bytesRemainingProgress: progress);
        Console.WriteLine("\rDownload complete.                                ");

        // Extract natives
        string nativesDir = System.IO.Path.Combine(DownloadTask.BasePath, "natives/", clientManifest.Id);
        DownloadTask.ExtractNatives(clientManifest, nativesDir);

        // Launch
        string javaExecutable = System.OperatingSystem.IsWindows() ? "javaw" : "java";
        Jvm jvm = new Jvm(javaExecutable);
        Console.WriteLine("Launching Minecraft...");
        LaunchMinecraft.Launch(jvm, account, clientManifest);
    }
}
