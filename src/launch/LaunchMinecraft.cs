
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperlaunch.Download;
using Hyperlaunch.Instances;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Launch;

public class LaunchMinecraft
{
    public static async Task<GameAccount> Auth()
    {
        string msToken = await MSAuth.Login();
        return await GameAccount.CreateAsync(msToken);
    }

    public static async Task DownloadEverything(ClientManifest clientManifest)
    {
        clientManifest.AssetIndex.Index.SaveInCorrectSpot();

        DownloadTask[] downloadTasks = DownloadTask.FromClientJson(clientManifest);
        Console.WriteLine($"{downloadTasks.ToList().Count} files to download");

        // foreach (DownloadTask task in downloadTasks)
        // {
        //     Log.Print(task.DestinationPath);
        // }

        var progress = new Progress<long>(bytesRemaining =>
        {
            double mbRemaining = bytesRemaining / (1024.0 * 1024.0);
            Console.Write($"\rDownload progress: {mbRemaining:F2} MB remaining   ");
        });

        await DownloadTask.ExecuteAllAsync(downloadTasks.ToList(), maxConcurrentNetwork: 16, bytesRemainingProgress: progress);
        Console.WriteLine("\rDownload complete.                                ");

        // Extract natives
        string nativesDir = System.IO.Path.Combine(DownloadTask.BasePath, "natives/", clientManifest.Id);
        DownloadTask.ExtractNatives(clientManifest, nativesDir);
    }

    public static async Task<ClientManifest> SetupClient(string versionId, bool forceModernJava)
    {
        await VersionManifest.LoadAsync();
        GameVersion version = VersionManifest.GetVersionById(versionId);
        Console.WriteLine($"Loading client manifest for {versionId}...");
        ClientManifest clientManifest = await ClientManifest.LoadFromVersionWithCacheAsync(version);
        // split off into more tasks here
        int requiredJava = clientManifest.JavaVersion?.MajorVersion ?? 8;
        
        await DownloadEverything(clientManifest);
        return clientManifest;
    }

    public static async Task<string> SetupClientAndResolveCmd(string versionId, Instance instance, bool forceModernJava)
    {
        Task<GameAccount> authTask = Auth();
        Task<List<Jvm>> scanJvmsTask = Jvm.ScanForJvms();
        Task<ClientManifest> setupClientTask = SetupClient(versionId, forceModernJava);
        
        await Task.WhenAll([authTask, setupClientTask, scanJvmsTask]);
        ClientManifest clientTaskOut = await setupClientTask;
        GameAccount gameAccount = await authTask;
        List<Jvm> jvms = await scanJvmsTask;
        int requiredJava = clientTaskOut.JavaVersion?.MajorVersion ?? 8;
        Jvm jvm = Jvm.FindBestForVersion(requiredJava, jvms);

        string cmd = ResolveCommand(jvm, gameAccount, clientTaskOut, instance);

        return cmd;
    }

    public static async Task AuthAndLaunch(string versionId, Instance instance, bool forceModernJava = false)
    {   
        Task<GameAccount> authTask = Auth();
        Task<List<Jvm>> scanJvmsTask = Jvm.ScanForJvms();
        Task<ClientManifest> setupClientTask = SetupClient(versionId, forceModernJava);
        
        await Task.WhenAll([authTask, setupClientTask, scanJvmsTask]);
        ClientManifest clientTaskOut = await setupClientTask;
        GameAccount gameAccount = await authTask;
        List<Jvm> jvms = await scanJvmsTask;
        int requiredJava = clientTaskOut.JavaVersion?.MajorVersion ?? 8;
        Jvm jvm = Jvm.FindBestForVersion(requiredJava, jvms);

        Launch(jvm, gameAccount, clientTaskOut, instance);
    }
    public static async Task AuthAndLaunch(ClientManifest clientManifest, Instance instance, bool forceModernJava = false)
    {   
        Task<GameAccount> authTask = Auth();
        Task<List<Jvm>> scanJvmsTask = Jvm.ScanForJvms();
        Task downloadEverythingTask = DownloadEverything(clientManifest);
        
        await Task.WhenAll([authTask, downloadEverythingTask, scanJvmsTask]);
        GameAccount gameAccount = await authTask;
        List<Jvm> jvms = await scanJvmsTask;
        int requiredJava = clientManifest.JavaVersion?.MajorVersion ?? 8;
        Jvm jvm = Jvm.FindBestForVersion(requiredJava, jvms);

        Launch(jvm, gameAccount, clientManifest, instance);
    }


    public static void Launch(Jvm jvm, GameAccount account, ClientManifest clientManifest, Instance instance, bool includeDefaultJvmArgs = false)
    {
        // resolve launch command from client manifest
        List<string> launchCommand = clientManifest.ResolveLaunchCommand(account, instance, includeDefaultJvmArgs);
        // if on windows, normalise all / to \ in arguments
        // if (OsInfo.Detect().Name == "windows")
        // {
        //     launchCommand = launchCommand.Select(arg => 
        //         arg.Replace("/", "\\").Replace("\\\\", "\\")).ToList();
        // }
        Log.Print("Resolved launch command");
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = jvm.ExecPath;
        // use ArgumentList to properly handle arguments with spaces
        foreach (var arg in launchCommand)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Log.Print(PrettyToString.FromList(launchCommand));

        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = instance.GetInstancePath();
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        
        var process = System.Diagnostics.Process.Start(startInfo);
        Log.Print($"{process.StandardOutput.ReadToEnd()}");
        Log.Print($"{process.ExitCode}");
    }

    public static string ResolveCommand(Jvm jvm, GameAccount account, ClientManifest clientManifest, Instance instance, bool includeDefaultJvmArgs = false)
    {
        // resolve launch command from client manifest
        List<string> launchCommand = clientManifest.ResolveLaunchCommand(account, instance, includeDefaultJvmArgs);
        // // if on windows, normalise all / to \ in arguments
        // if (OsInfo.Detect().Name == "windows")
        // {
        //     launchCommand = launchCommand.Select(arg => 
        //         arg.Replace("/", "\\").Replace("\\\\", "\\")).ToList();
        // }
        string res = "";
        foreach (string arg in launchCommand)
        {
            res += arg;
        }
        return res;
    }
}
